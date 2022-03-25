using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace YTVidoes.Core
{
    public class YoutubeService
    {
        private readonly ILogger<YoutubeService> _logger;
        private readonly IMemoryCache _cache;
        private readonly AppSettings _appSettings;

        public YoutubeService(ILogger<YoutubeService> logger,IMemoryCache cache,AppSettings appSettings)
        {
            _logger = logger;
            _cache = cache;
            _appSettings = appSettings;
        }
      
  

        public async Task<ServiceResult<YoutubeChannel>> GetYoutubeVideosInAChannel(string channelurl, IProgress<string>? progress = null,bool exportToCSV=false)
        {
            
            var channelId = await GetYouTubeChannelId(channelurl);
            if (string.IsNullOrEmpty(channelId))
            {
                return new ServiceResult<YoutubeChannel>()
                    { Errors = { new ValidationResult("Invalid channel url.") } };
            }
            
            var result = await GetVideosInChannel(channelId,progress);
            if (exportToCSV && result.IsValid)
            {
                try
                {
                    if (!Directory.Exists(_appSettings.ExportPath))
                        Directory.CreateDirectory(_appSettings.ExportPath);
                    var path = Path.Combine(_appSettings.ExportPath, $"{result.Result.Id}.csv");
                    using (var writer = new StreamWriter(path))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        await csv.WriteRecordsAsync(result.Result.Videos);
                    }
                    result.Result.DownloadPath = path;
                }
                catch (Exception e)
                {
                    result.AddError(e.Message);
                }
                
            }

            return result;
        }


        #region PrivateMethods

        private async Task<string> GetYouTubeChannelId(string url)
        {
            if (url.Contains("channel"))
                return url.Split('/').Last();

            var regex = @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?|watch)\/|.*[?&amp;]v=)|youtu\.be\/)([^""&amp;?\/ ]{11})";

            var match = Regex.Match(url, regex);
            var videoID = "";
            if (match.Success)
            {
                videoID = match.Groups[1].Value;
            }
            using (var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _appSettings.YouTubeAPIKey,
            }))
            {
                var searchRequest = youtubeService.Videos.List("snippet");
                searchRequest.Id = videoID;
                var searchResponse = await searchRequest.ExecuteAsync();

                var youTubeVideo = searchResponse.Items.FirstOrDefault();
                if (youTubeVideo != null)
                {
                    return youTubeVideo.Snippet.ChannelId;
                }
            }
            return url;
        }

        private async Task<ServiceResult<YoutubeChannel>> GetVideosInChannel(string channelId, IProgress<string>? progress = null)
        {

            var ytChannel = new YoutubeChannel();
            progress = progress ?? new Progress<string>();
            var videoDetails = new List<VideoDetails>();
            var result = new ServiceResult<YoutubeChannel>();
            if (!_cache.TryGetValue(channelId, out ytChannel))
            {
                videoDetails = new List<VideoDetails>();
                ytChannel = new YoutubeChannel();
                try
                {
                    var yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = _appSettings.YouTubeAPIKey });
                    var channelsListRequest = yt.Channels.List("contentDetails,snippet");
                    channelsListRequest.Id = channelId;

                    var channelsListResponse = await channelsListRequest.ExecuteAsync();
                    if (channelsListResponse == null || channelsListResponse.Items == null)
                    {
                        return new ServiceResult<YoutubeChannel>()
                        {
                            Errors =
                            {
                                new ValidationResult(
                                    "Youtube did not return any channel information. Please check the channel url.")
                            }
                        };
                    }

                    if (channelsListResponse.Items != null)
                    {
                        var ch = channelsListResponse.Items.FirstOrDefault();
                        ytChannel.Id = ch.Id;
                        ytChannel.Title = ch.Snippet.Title;
                        ytChannel.Url = "https://www.youtube.com/channel/" + ch.Id;
                        progress.Report("Fetching Videos for Channel " + ch.Snippet.Title);
                        foreach (var channel in channelsListResponse.Items)
                        {
                            // of videos uploaded to the user's channel.
                            var uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;
                            var nextPageToken = "";
                            while (nextPageToken != null)
                            {
                                var playlistItemsListRequest = yt.PlaylistItems.List("snippet");
                                playlistItemsListRequest.PlaylistId = uploadsListId;
                                playlistItemsListRequest.MaxResults = 50;
                                playlistItemsListRequest.PageToken = nextPageToken;
                                // Retrieve the list of videos uploaded to the user's channel.
                                var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();
                                ytChannel.TotalVideos = playlistItemsListResponse.PageInfo.TotalResults;
                                var pagedVideoDetals = new List<VideoDetails>();
                                int i = 0;
                                foreach (var playlistItem in playlistItemsListResponse.Items)
                                {
                                    i++;
                                    progress.Report($"Processsing {i} of {ytChannel.TotalVideos}");
                                    pagedVideoDetals.Add(new VideoDetails()
                                    {
                                        VideoId = playlistItem.Snippet.ResourceId.VideoId,
                                        VideoUrl = "https://www.youtube.com/embed/" + playlistItem.Snippet.ResourceId.VideoId,
                                        Title = playlistItem.Snippet.Title,
                                        Descriptions = playlistItem.Snippet.Description,
                                        ChannelName = playlistItem.Snippet.ChannelTitle,
                                        ChannelUrl = "https://www.youtube.com/channel/" + playlistItem.Snippet.ChannelId,
                                        ChannelId = playlistItem.Snippet.ChannelId,
                                        ImageUrl = playlistItem.Snippet.Thumbnails.High.Url,
                                        PostedDate = playlistItem.Snippet.PublishedAt
                                    });
                                }
                                await SetStatisticsForVideos(pagedVideoDetals);
                                videoDetails.AddRange(pagedVideoDetals);

                                nextPageToken = playlistItemsListResponse.NextPageToken;
                            }
                        }
                        var cacheExpiryOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTime.Now.AddHours(10),
                            Priority = CacheItemPriority.High,
                            SlidingExpiration = TimeSpan.FromHours(10)
                        };
                        ytChannel.Videos = videoDetails;
                        progress.Report($"Processsed {ytChannel.TotalVideos} of {ytChannel.TotalVideos}");
                        //setting cache entries
                        _cache.Set(ytChannel, videoDetails, cacheExpiryOptions);
                    }

                }
                catch (Exception e)
                {
                    result.AddError("Some exception occured" + e);

                }
            }

            result.Result = ytChannel;
            return result;
        }

        private async Task SetStatisticsForVideos(List<VideoDetails> videos)
        {
            using (var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _appSettings.YouTubeAPIKey,
            }))
            {
                var searchRequest = youtubeService.Videos.List("statistics");
                searchRequest.Id = new Repeatable<string>(videos.Select(c => c.VideoId));
                var searchResponse = await searchRequest.ExecuteAsync();

                foreach (var item in searchResponse.Items)
                {
                    foreach (var v in videos.Where(v => v.VideoId == item.Id))
                    {
                        if (item.Statistics != null)
                        {
                            v.ViewCount = item.Statistics.ViewCount;
                            v.LikeCount = item.Statistics.LikeCount;
                            v.CommentCount = item.Statistics.CommentCount;
                            v.DislikeCount = item.Statistics.DislikeCount;
                            v.FavoriteCount = item.Statistics.FavoriteCount;
                        }

                    }
                }

            }
        }

        #endregion

    }

}
