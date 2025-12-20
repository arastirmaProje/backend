namespace Personelim.Models
{
    public class Shift
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public decimal TotalHours { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}