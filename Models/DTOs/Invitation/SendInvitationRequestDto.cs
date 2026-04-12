namespace Personelim.DTOs.Invitation
{
    public class SendInvitationRequestDto
    {
        public Guid BusinessId { get; set; }
        public string Email { get; set; }
        public string? Message { get; set; }
    }
}