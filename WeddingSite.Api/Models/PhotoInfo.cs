using Microsoft.AspNetCore.Http.HttpResults;
using WeddingSite.Api.Data;

namespace WeddingSite.Api.Models
{
    public class PhotoInfo
    {
        public string Id { get; set; }

        public string ContentType { get; set; }

        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public string UserName { get; set; }


    }
}
