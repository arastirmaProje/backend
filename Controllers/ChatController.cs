using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Chat;
using Personelim.Services.Chat;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _service;

        public ChatController(IChatService service)
        {
            _service = service;
        }

        [HttpPost("personel")]
        public async Task<IActionResult> SendPersonel([FromBody] SendChatMessageRequestDto request)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var userJwt = ExtractBearerToken();
            var result = await _service.SendPersonelMessageAsync(userId, userJwt, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("yonetici")]
        public async Task<IActionResult> SendYonetici([FromBody] SendYoneticiChatRequestDto request)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var userJwt = ExtractBearerToken();
            var result = await _service.SendYoneticiMessageAsync(userId, userJwt, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private string? ExtractBearerToken()
        {
            var auth = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(auth)) return null;
            const string prefix = "Bearer ";
            return auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? auth.Substring(prefix.Length).Trim()
                : auth.Trim();
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.GetConversationsAsync(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("conversations/{conversationId}")]
        public async Task<IActionResult> GetConversation(Guid conversationId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.GetConversationDetailAsync(userId, conversationId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("conversations/{conversationId}")]
        public async Task<IActionResult> DeleteConversation(Guid conversationId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.DeleteConversationAsync(userId, conversationId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserIdFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c =>
                c.Type == "uid" || c.Type == "id" || c.Type == ClaimTypes.NameIdentifier);

            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }
}
