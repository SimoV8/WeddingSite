using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingSite.Api.Data;

namespace WeddingSite.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<MessagesController> logger;

        public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<MessagesController> logger)
        {
            this.context = context;
            this.userManager = userManager;
            this.logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetMessages()
        {
            var messages = await context.WeddingMessages
                .Include(m => m.User)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.AuthorName,
                    m.Message,
                    m.CreatedAt,
                    m.UserId
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var authorName = !string.IsNullOrWhiteSpace(request.AuthorName) 
                ? request.AuthorName 
                : user.FullName;

            var weddingMessage = new WeddingMessage
            {
                AuthorName = authorName,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                User = user
            };

            context.WeddingMessages.Add(weddingMessage);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessages), new { id = weddingMessage.Id }, new
            {
                weddingMessage.Id,
                weddingMessage.AuthorName,
                weddingMessage.Message,
                weddingMessage.CreatedAt,
                weddingMessage.UserId
            });
        }
    }

    public class CreateMessageRequest
    {
        public string Message { get; set; } = "";
        public string? AuthorName { get; set; }
    }
}