namespace PropertyManagement.API.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime Expiration { get; set; }
    }
}