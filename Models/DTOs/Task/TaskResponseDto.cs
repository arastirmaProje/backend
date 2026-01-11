using Personelim.Models.Enums;

namespace Personelim.DTOs.Task
{
    public class TaskResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AssignedToName { get; set; } 
        public string AssignedByName { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }     
        public string Difficulty { get; set; } 
        public string Thoughts { get; set; }
        public bool IsOverdue { get; set; }    
        public DateTime CreatedAt { get; set; }
    }
}