namespace Personelim.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public string? IslemYapildi { get; set; }
        public string? VeriJson { get; set; }
        public DateTime CreatedAt { get; set; }

        public ChatConversation? Conversation { get; set; }

        public ChatMessage()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
