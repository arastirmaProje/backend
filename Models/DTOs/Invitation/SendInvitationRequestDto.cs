namespace Personelim.DTOs.Invitation
{
    public class SendInvitationRequestDto
    {
        public Guid BusinessId { get; set; }
        public string Email { get; set; }
        public int? PositionId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? Message { get; set; }
    }
}