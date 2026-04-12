namespace Personelim.DTOs.Auth
{
    public class UpdateUserProfileRequestDto
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public IFormFile? Image { get; set; }
        
    }
}