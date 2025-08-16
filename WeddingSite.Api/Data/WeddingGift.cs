namespace WeddingSite.Api.Data
{
    public class WeddingGift
    {
        public int Id { get; set; }

        public required string Title { get; set; }

        public required string Description { get; set; }

        public required string ImageName { get; set; }

        public required string Cost { get; set; }

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
