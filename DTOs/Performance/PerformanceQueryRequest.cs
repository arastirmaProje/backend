namespace Personelim.DTOs.Performance
{
    public class PerformanceQueryRequest
    {
        public Guid BusinessId { get; set; }
        public Guid EmployeeUserId { get; set; }
        public DateTime StartDate { get; set; } // seçilen başlangıç
        public DateTime EndDate { get; set; }   // seçilen bitiş
    }
}