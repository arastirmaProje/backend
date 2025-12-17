namespace Personelim.DTOs.Shift
{
    public class CreateShiftRequest
    {
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; } 
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}