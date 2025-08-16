using WeddingSite.Api.Data;

namespace WeddingSite.Api.Services
{
    public interface IUserDBLog
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task LogAsync(ApplicationUser user, string message);
    }
}
