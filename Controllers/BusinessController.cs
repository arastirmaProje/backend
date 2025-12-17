using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Business;
using Personelim.Services.Business;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;
        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
        }
        
        [Authorize]
        [HttpPost("create-business")] 
        public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı." });

            var result = await _businessService.CreateBusinessAsync(request, userId);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(GetBusinessById), new { businessId = result.Data.Id }, result);
        }
        
        [Authorize]
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyBusiness([FromBody] VerifyBusinessRequest request)
        {
            var userId = GetUserIdFromToken(); 
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.VerifyBusinessAsync(userId, request);
        
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }
        
        [HttpGet] 
        public async Task<IActionResult> GetAllBusinesses()
        {
            // Eğer sadece giriş yapmışlar görsün istersen başına [Authorize] ekle.
            // Şu an herkese açık (Public) olarak yazdım.
            
            var result = await _businessService.GetAllBusinessesAsync();

            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        [Authorize]
        [HttpPut("{businessId}")] 
        public async Task<IActionResult> UpdateBusiness(Guid businessId, [FromForm] UpdateBusinessRequest request)
        {
            
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.UpdateBusinessAsync(userId, businessId, request);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{businessId}")]
        public async Task<IActionResult> GetBusinessById(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            
            var result = await _businessService.GetBusinessByIdAsync(userId == Guid.Empty ? null : userId, businessId);
            
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        private Guid GetUserIdFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "uid" || c.Type == "id" || c.Type == ClaimTypes.NameIdentifier);
            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }
}