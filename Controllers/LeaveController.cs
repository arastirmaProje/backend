using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Leave;
using Personelim.Helpers;
using Personelim.Services.Leave;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// İzin talep yönetimi. Oluşturma, listeleme ve onay/red işlemleri.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _leaveService;

        public LeaveController(ILeaveService leaveService)
        {
            _leaveService = leaveService;
        }

        /// <summary>Yeni izin talebi oluşturur.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceResponse<object>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<object>), 400)]
        public async Task<IActionResult> CreateLeave([FromBody] CreateLeaveRequestDto requestDto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _leaveService.CreateLeaveRequestAsync(userId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Giriş yapan kullanıcının belirli şirketteki izin taleplerini listeler.</summary>
        /// <param name="businessId">Şirket ID'si</param>
        [HttpGet("my-leaves/{businessId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMyLeaves(Guid businessId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _leaveService.GetMyLeavesAsync(userId, businessId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Şirketteki tüm izin taleplerini listeler.</summary>
        /// <remarks>**Abone değil:** Sadece Owner görebilir. **Abone:** Manager+</remarks>
        /// <param name="businessId">Şirket ID'si</param>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetBusinessLeaves(Guid businessId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _leaveService.GetBusinessLeavesAsync(userId, businessId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>İzin talebini onaylar veya reddeder.</summary>
        /// <remarks>**Abone değil:** Sadece Owner. **Abone:** Manager+</remarks>
        /// <param name="leaveId">İşlem yapılacak izin talebi ID'si</param>
        [HttpPut("{leaveId}/status")]
        [ProducesResponseType(typeof(ServiceResponse<object>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<object>), 400)]
        public async Task<IActionResult> UpdateStatus(Guid leaveId, [FromBody] UpdateLeaveStatusRequestDto requestDto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _leaveService.UpdateLeaveStatusAsync(userId, leaveId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>İzin talebini siler. Sadece talep sahibi silebilir.</summary>
        /// <param name="leaveId">Silinecek izin talebi ID'si</param>
        [HttpDelete("{leaveId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> DeleteLeave(Guid leaveId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _leaveService.DeleteLeaveAsync(userId, leaveId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
        }
    }
}
