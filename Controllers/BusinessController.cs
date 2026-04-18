using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Business;
using Personelim.Helpers;
using Personelim.Services.Business;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// Şirket (işletme) yönetimi. Oluşturma, güncelleme, doğrulama ve abonelik işlemleri.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;

        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
        }

        /// <summary>Yeni şirket oluşturur.</summary>
        /// <remarks>
        /// Şirket oluşturulduktan sonra e-posta ile doğrulama kodu gönderilir.
        /// <c>/api/business/verify</c> ile doğrulama tamamlanana kadar şirket aktif olmaz.
        /// </remarks>
        [Authorize]
        [HttpPost("create-business")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessResponseDto>), 201)]
        [ProducesResponseType(typeof(ServiceResponse<BusinessResponseDto>), 400)]
        public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.CreateBusinessAsync(requestDto, userId);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetBusinessById), new { businessId = result.Data!.Id }, result);
        }

        /// <summary>Şirketi e-posta doğrulama koduyla aktif eder.</summary>
        [Authorize]
        [HttpPost("verify")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> VerifyBusiness([FromBody] VerifyBusinessRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.VerifyBusinessAsync(userId, requestDto);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        /// <summary>Kullanıcının üyesi olduğu veya sahibi olduğu şirketleri listeler.</summary>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(ServiceResponse<List<BusinessResponseDto>>), 200)]
        public async Task<IActionResult> GetAllBusinesses()
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.GetAllBusinessesAsync(userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Şirket detaylarını getirir.</summary>
        /// <param name="businessId">Şirket ID'si</param>
        [HttpGet("{businessId}")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBusinessById(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            var result = await _businessService.GetBusinessByIdAsync(userId == Guid.Empty ? null : userId, businessId);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>Şirket bilgilerini günceller.</summary>
        /// <remarks>Sadece şirket sahibi (Owner) çağırabilir. multipart/form-data ile logo yüklenebilir.</remarks>
        /// <param name="businessId">Güncellenecek şirket ID'si</param>
        [Authorize]
        [HttpPut("{businessId}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<BusinessResponseDto>), 400)]
        public async Task<IActionResult> UpdateBusiness(Guid businessId, [FromForm] UpdateBusinessRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.UpdateBusinessAsync(userId, businessId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Şirket için premium abonelik başlatır.</summary>
        /// <remarks>
        /// Sadece şirket sahibi (Owner) çağırabilir.
        /// Abone olunca şu özellikler açılır:
        /// - Çalışanlara rol ve departman atanması
        /// - Departman yönetimi
        /// </remarks>
        /// <param name="businessId">Abone olunacak şirket ID'si</param>
        [Authorize]
        [HttpPost("{businessId}/subscribe")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> Subscribe(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.SubscribeAsync(userId, businessId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Şirketin premium aboneliğini iptal eder.</summary>
        /// <remarks>
        /// Sadece şirket sahibi (Owner) çağırabilir.
        /// İptal sonrası tüm çalışanlar Employee olarak görünür, departmanlar gizlenir.
        /// Veriler silinmez, tekrar abone olunursa geri gelir.
        /// </remarks>
        /// <param name="businessId">Aboneliği iptal edilecek şirket ID'si</param>
        [Authorize]
        [HttpPost("{businessId}/unsubscribe")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> Unsubscribe(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _businessService.UnsubscribeAsync(userId, businessId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Şirkete belge (PDF, DOC, resim) yükler.</summary>
        /// <param name="businessId">Şirket ID'si</param>
        [Authorize]
        [HttpPost("{businessId}/documents")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessDocumentResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<BusinessDocumentResponseDto>), 400)]
        public async Task<IActionResult> UploadBusinessDocument(Guid businessId, [FromForm] UploadBusinessDocumentRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            var result = await _businessService.UploadBusinessDocumentAsync(userId, businessId, requestDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Şirkete ait belgeleri listeler.</summary>
        /// <param name="businessId">Şirket ID'si</param>
        [Authorize]
        [HttpGet("{businessId}/documents")]
        [ProducesResponseType(typeof(ServiceResponse<List<BusinessDocumentResponseDto>>), 200)]
        public async Task<IActionResult> GetBusinessDocuments(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            var result = await _businessService.GetBusinessDocumentsAsync(userId, businessId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Şirkete ait belgeyi siler.</summary>
        /// <param name="documentId">Silinecek belge ID'si</param>
        [Authorize]
        [HttpDelete("documents/{documentId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
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
