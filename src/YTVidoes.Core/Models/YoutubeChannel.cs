namespace YTVidoes.Core;

public class YoutubeChannel
{
    public YoutubeChannel()
    {
        Videos = new List<VideoDetails>();
    }
    public string Id { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public int? TotalVideos { get; set; }
    public string DownloadPath { get; set; }
    public List<VideoDetails> Videos { get; set; }

}