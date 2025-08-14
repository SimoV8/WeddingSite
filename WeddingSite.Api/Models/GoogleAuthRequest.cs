using System.ComponentModel.DataAnnotations;

namespace WeddingSite.Api.Models
{
    public class GoogleAuthRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        
        public string? RedirectUri { get; set; }
    }
}