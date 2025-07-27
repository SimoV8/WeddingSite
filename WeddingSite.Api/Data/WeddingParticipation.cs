namespace WeddingSite.Api.Data
{
    public class WeddingParticipation
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public required string ParticipantFullName { get; set; }
        public int AgeCategory { get; set; } = 1;

        public required bool Present { get; set; } = false;

        public string Notes { get; set; } = string.Empty;

        public string UserId { get; set; } = "";

        public required ApplicationUser User { get; set; }
    }
}
