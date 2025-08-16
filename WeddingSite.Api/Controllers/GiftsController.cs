using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingSite.Api.Data;
using WeddingSite.Api.Services;

namespace WeddingSite.Api.Controllers
{
    [Authorize]
    public class GiftsController : Controller
    {
        private const string BUCKET_NAME = "wedding-vanessa-simone-bucket";
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IUserDBLog userDBLog;
        private readonly UserManager<ApplicationUser> userManager;

        public GiftsController(ApplicationDbContext applicationDbContext, IUserDBLog userDBLog, UserManager<ApplicationUser> userManager)
        {
            this.applicationDbContext = applicationDbContext;
            this.userDBLog = userDBLog;
            this.userManager = userManager;
        }

        /// <summary>
        /// Return the available wedding gifts
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetWeddingGiftsAsync()
        {
            return Ok(await applicationDbContext.WeddingGifts.ToListAsync());
        }

        /// <summary>
        /// Returns the pthoto with the specified id
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        [HttpGet("Img/{photoId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetImageAsync(string photoId)
        {
            var client = await StorageClient.CreateAsync();
            var stream = new MemoryStream();
            var obj = await client.DownloadObjectAsync(BUCKET_NAME, "gifts/" + photoId, stream);
            stream.Position = 0;
            return File(stream, obj.ContentType, obj.Name);
        }

        [HttpPut("{id}/Lock")]
        public async Task<IActionResult> LockGiftAsync(int id)
        {
            var gift = applicationDbContext.WeddingGifts.FirstOrDefault(x => x.Id == id);
            if(gift == null)
            {
                return NotFound("Gift not found");
            }

            if (!string.IsNullOrEmpty(gift.UserId))
            {
                return BadRequest("Gift already locked");
            }

            // Get the authenticated user
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            gift.UserId = user.Id;
            gift.User = user;

            
            this.applicationDbContext.WeddingGifts.Update(gift);
            await this.applicationDbContext.SaveChangesAsync();

            await userDBLog.LogAsync(user, $"Locked gift {id} ({gift.Title})");

            return Ok(gift);
        }

        [HttpPut("{id}/Unlock")]
        public async Task<IActionResult> UnlockGiftAsync(int id)
        {
            var gift = applicationDbContext.WeddingGifts.FirstOrDefault(x => x.Id == id);
            if (gift == null)
            {
                return NotFound("Gift not found");
            }

            if (string.IsNullOrEmpty(gift.UserId))
            {
                return BadRequest("Gift already unlocked");
            }

            // Get the authenticated user
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            gift.UserId = null;
            gift.User = null;


            this.applicationDbContext.WeddingGifts.Update(gift);
            await this.applicationDbContext.SaveChangesAsync();

            await userDBLog.LogAsync(user, $"Unlock gift {id} ({gift.Title})");

            return Ok(gift);
        }
    }
}
