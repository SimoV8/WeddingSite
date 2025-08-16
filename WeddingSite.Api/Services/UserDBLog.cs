using WeddingSite.Api.Data;

namespace WeddingSite.Api.Services
{
    public class UserDBLog : IUserDBLog
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<UserDBLog> logger;

        public UserDBLog(ApplicationDbContext context, ILogger<UserDBLog> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task LogAsync(ApplicationUser user, string message)
        {
            var log = new UserActionLog()
            {
                User = user,
                UserId = user.Id,
                Timestamp = DateTime.Now,
                LogMessage = message
            };

            this.logger.LogInformation($"User {user.Id} perfomed action: {message}");

            this.context.UserActionLogs.Add(log);
            await this.context.SaveChangesAsync();
        }
    }
}
