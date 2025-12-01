namespace Personelim.DTOs.BusinessMember
{
    public class AddEmployeeRequest
    {
        public Guid BusinessId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } = "Employee"; 
        public string Position { get; set; }
        public decimal? Salary { get; set; }
        public string TCIdentityNumber { get; set; }
    }
}