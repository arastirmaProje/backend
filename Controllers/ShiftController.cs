using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Shift;
using Personelim.Services.Shift;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public ShiftController(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        // START/END göndererek mesai kaydet
        // POST: /api/Shift
        [HttpPost]
        public async Task<IActionResult> SubmitShift([FromBody] SubmitShiftRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _shiftService.SubmitShiftAsync(userId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Giriş yapan kullanıcının mesaileri
        // GET: /api/Shift/my?businessId=...
        [HttpGet("my")]
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