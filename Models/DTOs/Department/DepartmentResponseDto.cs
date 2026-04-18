namespace Personelim.DTOs.Department
{
    public class DepartmentResponseDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
