using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Shift;
using Personelim.Helpers;
using Personelim.Services.Shift;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// Vardiya/mesai yönetimi. Çalışma saatlerini kaydetme ve listeleme.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public ShiftController(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        /// <summary>Vardiya kaydeder (giriş-çıkış saati).</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceResponse<object>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<object>), 400)]
        public async Task<IActionResult> SubmitShift([FromBody] SubmitShiftRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _shiftService.SubmitShiftAsync(userId, requestDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Giriş yapan kullanıcının belirli şirketteki vardiya kayıtlarını listeler.</summary>
        /// <param name="businessId">Şirket ID'si (query param olarak gönderilir)</param>
        [HttpGet("my")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMyShifts([FromQuery] Guid businessId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _shiftService.GetMyShiftsAsync(userId, businessId);
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
