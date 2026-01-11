namespace Personelim.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}