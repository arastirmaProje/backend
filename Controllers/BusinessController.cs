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
        public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı." });
            
            var result = await _businessService.CreateBusinessAsync(requestDto, userId);
            
            if (!result.Success) return BadRequest(result);

            return CreatedAtAction(nameof(GetBusinessById), new { businessId = result.Data.Id }, result);
        }
        
        [Authorize]
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyBusiness([FromBody] VerifyBusinessRequestDto requestDto)
        {
            var userId = GetUserIdFromToken(); 
            if (userId == Guid.Empty) return Unauthorized();
            
            var result = await _businessService.VerifyBusinessAsync(userId, requestDto);
        
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }
        
       
        
        [Authorize] 
        [HttpGet] 
        public async Task<IActionResult> GetAllBusinesses()
        {
           
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.GetAllBusinessesAsync(userId);
    
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{businessId}")] 
        public async Task<IActionResult> UpdateBusiness(Guid businessId, [FromForm] UpdateBusinessRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();
            
            var result = await _businessService.UpdateBusinessAsync(userId, businessId, requestDto);
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
        
        [Authorize]
        [HttpPost("{businessId}/documents")]
        public async Task<IActionResult> UploadBusinessDocument(Guid businessId, [FromForm] UploadBusinessDocumentRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            var result = await _businessService.UploadBusinessDocumentAsync(userId, businessId, requestDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpGet("{businessId}/documents")]
        public async Task<IActionResult> GetBusinessDocuments(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            var result = await _businessService.GetBusinessDocumentsAsync(userId, businessId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpDelete("documents/{documentId}")]
        public async Task<IActionResult> DeleteBusinessDocument(Guid documentId)
        {
            var userId = GetUserIdFromToken();
            var result = await _businessService.DeleteBusinessDocumentAsync(userId, documentId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserIdFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "uid" || c.Type == "id" || c.Type == ClaimTypes.NameIdentifier);
            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }
}