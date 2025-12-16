namespace Personelim.DTOs.Auth
{
    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        
        public string FirstName { get; set; }
        
        public string? ImageUrl { get; set; }
        public string LastName { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}