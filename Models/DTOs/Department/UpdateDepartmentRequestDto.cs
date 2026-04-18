namespace Personelim.DTOs.Department
{
    public class UpdateDepartmentRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
    }
}
