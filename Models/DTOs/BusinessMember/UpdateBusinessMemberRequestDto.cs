namespace Personelim.DTOs.BusinessMember
{
    public class UpdateBusinessMemberRequestDto
    {
        public int? PositionId { get; set; }
        public Guid? DepartmentId { get; set; }
        public decimal? Salary { get; set; }
        public string? TCIdentityNumber { get; set; }
    }
}
