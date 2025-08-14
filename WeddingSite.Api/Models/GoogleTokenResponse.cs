using System.Text.Json.Serialization;

namespace WeddingSite.Api.Models
{
    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
    }
    
    public class GoogleUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("verified_email")]
        public bool VerifiedEmail { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }
        
        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }
        
        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }
}