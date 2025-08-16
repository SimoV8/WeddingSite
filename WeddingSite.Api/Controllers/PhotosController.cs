using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WeddingSite.Api.Data;
using WeddingSite.Api.Models;
using WeddingSite.Api.Services;

namespace WeddingSite.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private const string BUCKET_NAME = "wedding-vanessa-simone-bucket";
        private readonly ApplicationDbContext applicationDbContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IUserDBLog userDBLog;
        private readonly ILogger<MessagesController> logger;

        public PhotosController(ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager, IUserDBLog userDBLog, ILogger<MessagesController> logger)
        {
            this.applicationDbContext = applicationDbContext;
            this.userManager = userManager;
            this.userDBLog = userDBLog;
            this.logger = logger;
        }

        /// <summary>
        /// Return the list of all available photos with their metadata
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetPhotos()
        {
            var photos = applicationDbContext.UserUploadedPhotos.Select(photo => new PhotoInfo()
            {
                Id = photo.FileName,
                ContentType = photo.ContentType,
                UserId = photo.UserId,
                CreatedAt = photo.UploadedAt,
                UserName = photo.User.FullName,
            }).ToList();

            photos = photos.OrderByDescending(p => p.CreatedAt).ToList();

            return Ok(photos);
        }

        /// <summary>
        /// Returns the pthoto with the specified id
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        [HttpGet("{photoId}")]
        public async Task<IActionResult> GetPhoto(string photoId)
        {
            var client = await StorageClient.CreateAsync();
            var stream = new MemoryStream();
            var obj = await client.DownloadObjectAsync(BUCKET_NAME, photoId, stream);
            stream.Position = 0;
            return File(stream, obj.ContentType, obj.Name);
        }

        /// <summary>
        /// Upload a new photo. The content type is required to specify the file extention (Only jpeg, png and webp files are accepted).
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost()]
        [Authorize]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // 1. Validate the file
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // 2. Infer the file extension from the Content-Type header
            var fileExtension = GetFileExtensionFromContentType(file.ContentType);
            if (string.IsNullOrEmpty(fileExtension))
            {
                return BadRequest("Unsupported file type.");
            }

            // 3. Get the authenticated user
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            // 4. Generate a unique filename based on user ID and timestamp
            var timestamp = System.DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var fileName = $"{user.Id}-{timestamp}{fileExtension}";
            

            // 5. Save the file to the google cloud bucket asynchronously
            try
            {
                logger.LogInformation($"Uploading file '{fileName}'");
                await userDBLog.LogAsync(user, $"Uploading file '{fileName}'");

                var client = await StorageClient.CreateAsync();
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    var res = await client.UploadObjectAsync(BUCKET_NAME, fileName, file.ContentType, stream);
                }

                // 6. Insert record in DB for treciability
                var uploadedPhoto = new UserUploadedPhoto()
                {
                    FileName = fileName,
                    ContentType = file.ContentType,
                    UploadedAt = DateTime.Now,
                    UserId = user.Id,
                    User = user
                };

                applicationDbContext.UserUploadedPhotos.Add(uploadedPhoto);
                await applicationDbContext.SaveChangesAsync();

                logger.LogInformation($"File '{fileName}' uploaded successfully");

                // 7. Return a success response
                return Ok(new PhotoInfo()
                {
                    Id = uploadedPhoto.FileName,
                    ContentType = uploadedPhoto.ContentType,
                    UserId = uploadedPhoto.UserId,
                    CreatedAt = uploadedPhoto.UploadedAt,
                    UserName = uploadedPhoto.User.FullName,
                });

            }
            catch (IOException ex)
            {
                logger.LogError(ex, $"File upload of '{fileName}' failed");
                // LogAsync the exception for debugging
                return StatusCode(500, $"An error occurred while saving the file: {ex.Message}");
            }    
        }

        /// <summary>
        /// Delete the photo with the given photoID. The operation succeed only if the photo is ownned by the authenticated user
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        [HttpDelete("{photoId}")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string photoId)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var uploadedPhoto = applicationDbContext.UserUploadedPhotos.FirstOrDefault(u => u.UserId == user.Id && u.FileName == photoId);

            if(uploadedPhoto == null)
            {
                return NotFound("Photo not found");
            }

            await userDBLog.LogAsync(user, $"Deleting file '{uploadedPhoto.FileName}'");

            var client = await StorageClient.CreateAsync();
            await client.DeleteObjectAsync(BUCKET_NAME, photoId);

            applicationDbContext.UserUploadedPhotos.Remove(uploadedPhoto);
            await applicationDbContext.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// A helper method to get a file extension from a MIME type.
        /// </summary>
        /// <param name="contentType">The MIME type (e.g., "image/jpeg").</param>
        /// <returns>The corresponding file extension (e.g., ".jpg") or null if not found.</returns>
        private string? GetFileExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => null, // Return null for unsupported types
            };
        }
    }
}
