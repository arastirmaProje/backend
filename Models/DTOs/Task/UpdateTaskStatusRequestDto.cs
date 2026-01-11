using Personelim.Models.Enums;

namespace Personelim.DTOs.Task
{
    public class UpdateTaskStatusRequestDto
    {
        public string? Status { get; set; }
        public string? Thoughts { get; set; } 
        public string? Difficulty { get; set; }
    }
}