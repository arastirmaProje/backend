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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı." });
            }

            var userId = Guid.Parse(userIdClaim.Value);
            
            var result = await _businessService.CreateBusinessAsync(request, userId);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetBusinessById), new { businessId = result.Data.Id }, result);
        }
        
        
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyBusiness([FromBody] VerifyBusinessRequest request)
        {
            var userId = GetUserIdFromToken(); 

            var result = await _businessService.VerifyBusinessAsync(userId, request);
        
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }
        
        private Guid GetUserIdFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "uid" || c.Type == "id" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
        
        
        [HttpGet("{businessId}")]
        public async Task<IActionResult> GetBusinessById(Guid businessId)
        {
            Guid? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null)
            {
                userId = Guid.Parse(userIdClaim.Value);
            }

            var result = await _businessService.GetBusinessByIdAsync(userId, businessId);
            
            if (!result.Success)
            {
                return NotFound(result);
            }
            
            return Ok(result);
        }
    }
}