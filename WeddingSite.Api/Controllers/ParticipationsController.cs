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
    public class ParticipationsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<MessagesController> logger;

        public ParticipationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<MessagesController> logger)
        {
            this.context = context;
            this.userManager = userManager;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetParticipationsAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var weddingParticipations = await context.WeddingParticipations
                .Where(m => m.UserId == user.Id)
                .Include(m => m.User)
                .OrderByDescending(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.ParticipantFullName,
                    m.AgeCategory,
                    m.Present,
                    m.Notes,
                })
                .ToListAsync();

            return Ok(weddingParticipations);
        }

        [HttpPost]
        public async Task<IActionResult> CreateParticipationAsync([FromBody] CreateParticipationRequest request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var weddingParticipation = new WeddingParticipation
            {
                ParticipantFullName = request.ParticipantFullName,
                AgeCategory = request.AgeCategory,
                Present = request.Present,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                User = user
            };

            context.WeddingParticipations.Add(weddingParticipation);
            await context.SaveChangesAsync();

            return Ok(new
            {
                weddingParticipation.Id,
                weddingParticipation.ParticipantFullName,
                weddingParticipation.AgeCategory,
                weddingParticipation.Present,
                weddingParticipation.Notes,
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditParticipationAsync(int id, [FromBody] CreateParticipationRequest request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var weddingParticipation = context.WeddingParticipations.Where(m => m.UserId == user.Id).FirstOrDefault(p => p.Id == id);

            if (weddingParticipation == null)
            {
                return NotFound();
            }

            weddingParticipation.ParticipantFullName = request.ParticipantFullName;
            weddingParticipation.AgeCategory = request.AgeCategory;
            weddingParticipation.Present = request.Present;
            weddingParticipation.Notes = request.Notes;

            await context.SaveChangesAsync();

            return Ok(new
            {
                weddingParticipation.Id,
                weddingParticipation.ParticipantFullName,
                weddingParticipation.AgeCategory,
                weddingParticipation.Present,
                weddingParticipation.Notes,
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var weddingParticipation = context.WeddingParticipations.Where(m => m.UserId == user.Id).FirstOrDefault(p => p.Id == id);

            if (weddingParticipation == null)
            {
                return NotFound();
            }

            context.WeddingParticipations.Remove(weddingParticipation);

            await context.SaveChangesAsync();

            return Ok();
        }




    }

    public class CreateParticipationRequest
    {
        public required string ParticipantFullName { get; set; }
        public required int AgeCategory { get; set; } = 0;

        public required bool Present { get; set; }

        public required string Notes { get; set; }
    }
}
