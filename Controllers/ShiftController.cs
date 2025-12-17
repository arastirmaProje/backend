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

        [HttpPost("create")]
        public async Task<IActionResult> CreateShift([FromBody] CreateShiftRequest request)
        {
            var userId = GetUserIdFromToken();
            var result = await _shiftService.CreateShiftAsync(userId, request);
            
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("business/{businessId}")]
        public async Task<IActionResult> GetShiftsByBusiness(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            var result = await _shiftService.GetShiftsByBusinessAsync(userId, businessId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

       
        private Guid GetUserIdFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "uid" || c.Type == "id" || c.Type == ClaimTypes.NameIdentifier);
            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }
}