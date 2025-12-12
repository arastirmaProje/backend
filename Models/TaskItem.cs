using System.ComponentModel.DataAnnotations;
using Personelim.Models.Enums;
using TaskStatus = Personelim.Models.Enums.TaskStatus;

namespace Personelim.Models
{
    public class TaskItem
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; } 
        public Business Business { get; set; }

        public Guid AssignedByUserId { get; set; } 
        public User AssignedByUser { get; set; }

        public Guid AssignedToUserId { get; set; } 
        public User AssignedToUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } 

        public string Description { get; set; } 

        public DateTime StartDate { get; set; } = DateTime.UtcNow; 
        public DateTime EndDate { get; set; } 

        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public TaskDifficulty Difficulty { get; set; } = TaskDifficulty.Medium; 
        public string? Thoughts { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}