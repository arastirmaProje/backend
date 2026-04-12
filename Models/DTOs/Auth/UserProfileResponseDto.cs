namespace Personelim.DTOs.Auth
{
    public class UserProfileResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int BusinessCount { get; set; }  
        public int OwnedBusinessCount { get; set; }  
    }
}