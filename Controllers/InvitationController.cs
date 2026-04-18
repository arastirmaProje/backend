using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Invitation;
using Personelim.Helpers;
using Personelim.Services.Invitation;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// Davetiye işlemleri. Mevcut kullanıcılara veya e-posta ile yeni kullanıcılara şirket daveti gönderir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        public InvitationController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        /// <summary>Şirkete davet gönderir.</summary>
        /// <remarks>
        /// Sistemde kayıtlı kullanıcıya davet: anlık olarak şirkete eklenir.
        /// Yeni kullanıcıya davet: e-posta ile davet linki gönderilir, kabul edince eklenir.
        /// </remarks>
        [HttpPost("send")]
        [ProducesResponseType(typeof(ServiceResponse<object>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<object>), 400)]
        public async Task<IActionResult> SendInvitation([FromBody] SendInvitationRequestDto requestDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _invitationService.SendInvitationAsync(userId, requestDto);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Giriş yapan kullanıcıya gelen bekleyen davetleri listeler.</summary>
        /// <remarks>Kullanıcı kendi e-posta adresine gelen tüm aktif davetleri görür.</remarks>
        [HttpGet("my-invitations")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMyInvitations()
        {
            var userEmailClaim = User.FindFirst(ClaimTypes.Email);
            if (userEmailClaim == null) return Unauthorized("Token içinde email bulunamadı");

            var email = userEmailClaim.Value;
            var result = await _invitationService.GetUserInvitationsAsync(email);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}
