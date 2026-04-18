namespace Personelim.DTOs.BusinessMember
{
    public class AddEmployeeRequestDto
    {
        public Guid BusinessId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int PositionId { get; set; }
        public Guid? DepartmentId { get; set; }
        public decimal? Salary { get; set; }
        public string? TCIdentityNumber { get; set; }
    }
}
