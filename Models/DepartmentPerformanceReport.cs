namespace Personelim.Models
{
    public class DepartmentPerformanceReport
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid RequestedByUserId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string DepartmanAdi { get; set; } = string.Empty;
        public double DepartmanSkoru { get; set; }
        public int ToplamCalisan { get; set; }
        public string? Summary { get; set; }
        public string? DetailedReport { get; set; }
        public string AiRequestJson { get; set; } = string.Empty;
        public string AiResponseJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public Business? Business { get; set; }
        public Department? Department { get; set; }

        public DepartmentPerformanceReport()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
