namespace jool_backend.DTOs
{
    public class HashtagDto
    {
        public int hashtag_id { get; set; }
        public string name { get; set; } = string.Empty;
        public int used_count { get; set; }
    }
}