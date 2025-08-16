namespace WeddingSite.Api.Data
{
    public class UserUploadedPhoto
    {
        public int Id { get; set; }

        public required string FileName { get; set; }

        public required string ContentType { get; set; }

        public required DateTime UploadedAt { get; set; }

        public required string UserId { get; set; }

        public ApplicationUser User { get; set; }
    }
}
