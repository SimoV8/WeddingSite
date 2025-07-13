namespace WeddingSite.Api.Data
{
    public class WeddingMessage
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
    }
}
