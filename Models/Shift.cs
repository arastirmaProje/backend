using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Personelim.Models
{
    public class Shift
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }
        public Business Business { get; set; }

        public Guid UserId { get; set; } 
        public User User { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}