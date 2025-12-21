namespace Personelim.Models
{
    public class PerformanceReport
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }
        public Guid EmployeeUserId { get; set; }       
        public Guid RequestedByUserId { get; set; }    

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public int CompletedTaskCount { get; set; }
        public int NotCompletedTaskCount { get; set; }
        public double TargetWorkHours { get; set; }
        public double RealizedWorkHours { get; set; }
        public int UsedLeaveDays { get; set; }

        public double PerformanceScore { get; set; }
        public string? Summary { get; set; }
        public string? DetailedReport { get; set; }

       
        public string AiRequestJson { get; set; } = "{}";
        public string AiResponseJson { get; set; } = "{}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}