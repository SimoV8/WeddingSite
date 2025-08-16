namespace WeddingSite.Api.Data
{
    public class UserRefreshToken
    {
        public int Id { get; set; }

        public required string RefreshToken { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required DateTime ExpiresAt { get; set; }

        public required string UserId { get; set; } = "";

        public ApplicationUser User { get; set; } 
    }
}
