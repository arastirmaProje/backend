using Personelim.Models.Enums;

namespace Personelim.DTOs.Task
{
    public class CreateTaskRequest
    {
        public Guid BusinessId { get; set; }
        public Guid AssignedToUserId { get; set; } 
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}