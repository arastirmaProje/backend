namespace Personelim.DTOs.Performance
{
    public class PerformanceReportListItem
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid EmployeeUserId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public double PerformanceScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}