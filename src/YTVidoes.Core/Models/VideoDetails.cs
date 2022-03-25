namespace YTVidoes.Core
{
    public class VideoDetails
    {
        public string VideoId { get; set; }
        public string VideoUrl { get; set; }
        public string Title { get; set; }
        public string Descriptions { get; set; }
        public string ImageUrl { get; set; }
        public string ChannelName { get; set; }
        public string ChannelUrl { get; set; }
        public string ChannelId { get; set; }
        public DateTime? PostedDate { get; set; }
        public ulong? ViewCount { get; set; }
        public ulong? LikeCount { get; set; }
        public ulong? CommentCount { get; set; }
        public ulong? FavoriteCount { get; set; }
        public ulong? DislikeCount { get; set; }


    }
   
}