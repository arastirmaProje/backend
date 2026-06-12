using System.Text.Json;
using System.Text.Json.Serialization;

namespace Personelim.DTOs.Chat
{
    public class SendChatMessageRequestDto
    {
        public Guid? ConversationId { get; set; }
        public Guid? BusinessId { get; set; }
        public string Mesaj { get; set; } = string.Empty;
    }

    public class SendYoneticiChatRequestDto
    {
        public Guid? ConversationId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? DepartmanId { get; set; }
        public string Mesaj { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public Guid ConversationId { get; set; }
        public string Yanit { get; set; } = string.Empty;
        public string? IslemYapildi { get; set; }
        public JsonElement? Veri { get; set; }
    }

    public class ConversationListItemDto
    {
        public Guid Id { get; set; }
        public string ChatType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? DepartmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
        public string? LastMessagePreview { get; set; }
    }

    public class ConversationDetailDto
    {
        public Guid Id { get; set; }
        public string ChatType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? DepartmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? IslemYapildi { get; set; }
        public JsonElement? Veri { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── AI servisine gönderdiğimiz body ──────────────────────────────────────

    public class AiChatMesajDto
    {
        [JsonPropertyName("rol")] public string Rol { get; set; } = string.Empty;
        [JsonPropertyName("icerik")] public string Icerik { get; set; } = string.Empty;
    }

    public class AiPersonelChatIstegiDto
    {
        [JsonPropertyName("kullanici_id")] public Guid KullaniciId { get; set; }
        [JsonPropertyName("mesaj")] public string Mesaj { get; set; } = string.Empty;
        [JsonPropertyName("gecmis")] public List<AiChatMesajDto> Gecmis { get; set; } = new();
        [JsonPropertyName("user_token")] public string? UserToken { get; set; }
    }

    public class AiYoneticiChatIstegiDto
    {
        [JsonPropertyName("kullanici_id")] public Guid KullaniciId { get; set; }
        [JsonPropertyName("departman_id")] public Guid? DepartmanId { get; set; }
        [JsonPropertyName("mesaj")] public string Mesaj { get; set; } = string.Empty;
        [JsonPropertyName("gecmis")] public List<AiChatMesajDto> Gecmis { get; set; } = new();
        [JsonPropertyName("user_token")] public string? UserToken { get; set; }
    }

    public class AiChatYanitiDto
    {
        [JsonPropertyName("yanit")] public string Yanit { get; set; } = string.Empty;
        [JsonPropertyName("islem_yapildi")] public string? IslemYapildi { get; set; }
        [JsonPropertyName("veri")] public JsonElement? Veri { get; set; }
    }
}
