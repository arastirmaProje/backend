using Personelim.DTOs.Chat;
using Personelim.Helpers;

namespace Personelim.Services.Chat
{
    public interface IChatService
    {
        Task<ServiceResponse<ChatResponseDto>> SendPersonelMessageAsync(Guid currentUserId, SendChatMessageRequestDto request);
        Task<ServiceResponse<ChatResponseDto>> SendYoneticiMessageAsync(Guid currentUserId, SendYoneticiChatRequestDto request);
        Task<ServiceResponse<List<ConversationListItemDto>>> GetConversationsAsync(Guid currentUserId);
        Task<ServiceResponse<ConversationDetailDto>> GetConversationDetailAsync(Guid currentUserId, Guid conversationId);
        Task<ServiceResponse<bool>> DeleteConversationAsync(Guid currentUserId, Guid conversationId);
    }
}
