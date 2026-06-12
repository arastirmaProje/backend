namespace Personelim.Models
{
    public class ChatConversation
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string ChatType { get; set; } = "personel";
        public string? Title { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
        public Business? Business { get; set; }
        public Department? Department { get; set; }
        public ICollection<ChatMessage> Messages { get; set; }

        public ChatConversation()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
            Messages = new List<ChatMessage>();
        }
    }
}
