using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Chat;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Resources;

namespace Personelim.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpPersonel;
        private readonly HttpClient _httpYonetici;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ChatService(AppDbContext context, IHttpClientFactory factory, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
            _httpPersonel = factory.CreateClient("AiChatPersonel");
            _httpYonetici = factory.CreateClient("AiChatYonetici");
        }

        public async Task<ServiceResponse<ChatResponseDto>> SendPersonelMessageAsync(Guid currentUserId, SendChatMessageRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Mesaj))
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["MessageRequired"]);

                var conversation = await GetOrCreateConversationAsync(currentUserId, request.ConversationId, "personel", request.BusinessId, null);
                if (conversation == null)
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["UnauthorizedAction"]);

                var gecmis = await BuildHistoryAsync(conversation.Id);

                var aiBody = new AiPersonelChatIstegiDto
                {
                    KullaniciId = currentUserId,
                    Mesaj = request.Mesaj,
                    Gecmis = gecmis
                };

                var resp = await _httpPersonel.PostAsJsonAsync("", aiBody);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["AiServiceError"], body);
                }

                var aiResult = await resp.Content.ReadFromJsonAsync<AiChatYanitiDto>();
                if (aiResult == null)
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["AiResponseReadError"]);

                await PersistTurnAsync(conversation, request.Mesaj, aiResult);

                return ServiceResponse<ChatResponseDto>.SuccessResult(new ChatResponseDto
                {
                    ConversationId = conversation.Id,
                    Yanit = aiResult.Yanit,
                    IslemYapildi = aiResult.IslemYapildi,
                    Veri = aiResult.Veri
                });
            }
            catch (Exception ex)
            {
                return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<ChatResponseDto>> SendYoneticiMessageAsync(Guid currentUserId, SendYoneticiChatRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Mesaj))
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["MessageRequired"]);

                if (request.BusinessId == Guid.Empty)
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["BusinessIdRequired"]);

                var isOwner = await _context.Businesses.AnyAsync(b => b.Id == request.BusinessId && b.OwnerId == currentUserId);
                var membership = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.BusinessId == request.BusinessId && bm.UserId == currentUserId && bm.IsActive);
                var isManager = membership != null && JobTitles.GetRole(membership.Position) >= UserRole.Manager;

                if (!isOwner && !isManager)
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["UnauthorizedAction"]);

                var conversation = await GetOrCreateConversationAsync(currentUserId, request.ConversationId, "yonetici", request.BusinessId, request.DepartmanId);
                if (conversation == null)
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["UnauthorizedAction"]);

                var gecmis = await BuildHistoryAsync(conversation.Id);

                var aiBody = new AiYoneticiChatIstegiDto
                {
                    KullaniciId = currentUserId,
                    DepartmanId = request.DepartmanId,
                    Mesaj = request.Mesaj,
                    Gecmis = gecmis
                };

                var resp = await _httpYonetici.PostAsJsonAsync("", aiBody);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["AiServiceError"], body);
                }

                var aiResult = await resp.Content.ReadFromJsonAsync<AiChatYanitiDto>();
                if (aiResult == null)
                    return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["AiResponseReadError"]);

                await PersistTurnAsync(conversation, request.Mesaj, aiResult);

                return ServiceResponse<ChatResponseDto>.SuccessResult(new ChatResponseDto
                {
                    ConversationId = conversation.Id,
                    Yanit = aiResult.Yanit,
                    IslemYapildi = aiResult.IslemYapildi,
                    Veri = aiResult.Veri
                });
            }
            catch (Exception ex)
            {
                return ServiceResponse<ChatResponseDto>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<List<ConversationListItemDto>>> GetConversationsAsync(Guid currentUserId)
        {
            try
            {
                var convs = await _context.ChatConversations
                    .Where(c => c.UserId == currentUserId && c.IsActive)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Select(c => new ConversationListItemDto
                    {
                        Id = c.Id,
                        ChatType = c.ChatType,
                        Title = c.Title,
                        BusinessId = c.BusinessId,
                        DepartmentId = c.DepartmentId,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        MessageCount = c.Messages.Count,
                        LastMessagePreview = c.Messages
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => m.Content)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return ServiceResponse<List<ConversationListItemDto>>.SuccessResult(convs);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<ConversationListItemDto>>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<ConversationDetailDto>> GetConversationDetailAsync(Guid currentUserId, Guid conversationId)
        {
            try
            {
                var conv = await _context.ChatConversations
                    .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                    .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive);

                if (conv == null)
                    return ServiceResponse<ConversationDetailDto>.ErrorResult(_localizer["NotFound"]);

                if (conv.UserId != currentUserId)
                    return ServiceResponse<ConversationDetailDto>.ErrorResult(_localizer["UnauthorizedAction"]);

                var detail = new ConversationDetailDto
                {
                    Id = conv.Id,
                    ChatType = conv.ChatType,
                    Title = conv.Title,
                    BusinessId = conv.BusinessId,
                    DepartmentId = conv.DepartmentId,
                    CreatedAt = conv.CreatedAt,
                    UpdatedAt = conv.UpdatedAt,
                    Messages = conv.Messages.Select(m => new ChatMessageDto
                    {
                        Id = m.Id,
                        Role = m.Role,
                        Content = m.Content,
                        IslemYapildi = m.IslemYapildi,
                        Veri = ParseVeri(m.VeriJson),
                        CreatedAt = m.CreatedAt
                    }).ToList()
                };

                return ServiceResponse<ConversationDetailDto>.SuccessResult(detail);
            }
            catch (Exception ex)
            {
                return ServiceResponse<ConversationDetailDto>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteConversationAsync(Guid currentUserId, Guid conversationId)
        {
            try
            {
                var conv = await _context.ChatConversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conv == null)
                    return ServiceResponse<bool>.ErrorResult(_localizer["NotFound"]);

                if (conv.UserId != currentUserId)
                    return ServiceResponse<bool>.ErrorResult(_localizer["UnauthorizedAction"]);

                conv.IsActive = false;
                conv.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        // ── Internal helpers ────────────────────────────────────────────────

        private async Task<ChatConversation?> GetOrCreateConversationAsync(Guid userId, Guid? conversationId, string chatType, Guid? businessId, Guid? departmentId)
        {
            if (conversationId.HasValue)
            {
                var existing = await _context.ChatConversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.IsActive);
                if (existing == null) return null;
                if (existing.UserId != userId) return null;
                return existing;
            }

            var conv = new ChatConversation
            {
                UserId = userId,
                BusinessId = businessId,
                DepartmentId = departmentId,
                ChatType = chatType
            };
            _context.ChatConversations.Add(conv);
            await _context.SaveChangesAsync();
            return conv;
        }

        private async Task<List<AiChatMesajDto>> BuildHistoryAsync(Guid conversationId)
        {
            return await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AiChatMesajDto { Rol = m.Role, Icerik = m.Content })
                .ToListAsync();
        }

        private async System.Threading.Tasks.Task PersistTurnAsync(ChatConversation conv, string userMessage, AiChatYanitiDto aiResult)
        {
            var userMsg = new ChatMessage
            {
                ConversationId = conv.Id,
                Role = "user",
                Content = userMessage
            };

            var modelMsg = new ChatMessage
            {
                ConversationId = conv.Id,
                Role = "model",
                Content = aiResult.Yanit,
                IslemYapildi = aiResult.IslemYapildi,
                VeriJson = aiResult.Veri.HasValue ? aiResult.Veri.Value.GetRawText() : null
            };

            _context.ChatMessages.Add(userMsg);
            _context.ChatMessages.Add(modelMsg);

            if (string.IsNullOrEmpty(conv.Title))
                conv.Title = userMessage.Length > 60 ? userMessage[..60] + "..." : userMessage;
            conv.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private static JsonElement? ParseVeri(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch
            {
                return null;
            }
        }
    }
}
