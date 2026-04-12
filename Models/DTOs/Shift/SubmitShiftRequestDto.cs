namespace Personelim.DTOs.Shift
{
    public class SubmitShiftRequestDto
    {
        public Guid BusinessId { get; set; }
        public DateTime StartTime { get; set; } 
        public DateTime EndTime { get; set; }   
    }
}