namespace Personelim.DTOs.Performance
{
    public class PerformanceQueryRequestDto
    {
        public Guid BusinessId { get; set; }
        public Guid EmployeeUserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }   
    }
}