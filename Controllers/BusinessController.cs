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