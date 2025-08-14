using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WeddingSite.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private const string BUCKET_NAME = "wedding-vanessa-simone-bucket";

        [HttpGet]
        public async Task<IEnumerable<string>> GetPhotos()
        {
            var client = await StorageClient.CreateAsync();
            var storageObjects = client.ListObjects(BUCKET_NAME);

            return storageObjects.Select(obj => obj.Name);
        }

        [HttpGet("{photoId}")]
        public async Task<IActionResult> GetPhoto(string photoId)
        {
            var client = await StorageClient.CreateAsync();
            var stream = new MemoryStream();
            var obj = await client.DownloadObjectAsync(BUCKET_NAME, photoId, stream);
            stream.Position = 0;
            return File(stream, obj.ContentType, obj.Name);
        }
    }
}
