using Personelim.Models.Enums;

namespace Personelim.DTOs.Task
{
    public class TaskResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AssignedToName { get; set; } // Personel Adı
        public string AssignedByName { get; set; } // Görevi Veren Adı
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }     // Enum string dönüşümü
        public string Difficulty { get; set; } // Enum string dönüşümü
        public string Thoughts { get; set; }
        public bool IsOverdue { get; set; }    // Süresi geçmiş mi?
        public DateTime CreatedAt { get; set; }
    }
}