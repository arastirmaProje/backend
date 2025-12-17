namespace Personelim.DTOs.Shift
{
    public class ShiftResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } 
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double TotalHours { get; set; } 
       
        public DateTime CreatedAt { get; set; }
    }
}