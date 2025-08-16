namespace WeddingSite.Api.Data
{
    public class UserActionLog
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public DateTime Timestamp { get; set; }

        public string LogMessage { get; set; }

        public ApplicationUser User { get; set; }
    }
}
