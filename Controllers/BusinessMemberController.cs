using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.BusinessMember;
using Personelim.Services.BusinessMember;
using System.Security.Claims;
using Personelim.Helpers;

namespace Personelim.Controllers
{
    /// <summary>
    /// Çalışan (personel) yönetimi. Listeleme, güncelleme, çıkarma ve belge işlemleri.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class BusinessMemberController : ControllerBase
    {
        private readonly IBusinessMemberService _memberService;

        public BusinessMemberController(IBusinessMemberService memberService)
        {
            _memberService = memberService;
        }

        /// <summary>Şirkete ait tüm aktif çalışanları listeler.</summary>
        /// <remarks>
        /// Şirket abone değilse tüm çalışanlar <c>Position: "Diğer"</c>, <c>PermissionLevel: "Employee"</c> olarak döner.
        /// Şirket abone ise gerçek pozisyon, rol ve departman bilgileri döner.
        /// </remarks>
        /// <param name="businessId">Şirket ID'si</param>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(ServiceResponse<List<BusinessMemberResponseDto>>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<List<BusinessMemberResponseDto>>), 400)]
        public async Task<IActionResult> GetMembersByBusiness(Guid businessId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.GetMembersByBusinessIdAsync(userId, businessId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Tek bir çalışanın detaylarını getirir.</summary>
        /// <param name="memberId">BusinessMember ID'si (User ID değil)</param>
        [HttpGet("{memberId}")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto>), 400)]
        public async Task<IActionResult> GetMemberById(Guid memberId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.GetMemberByIdAsync(userId, memberId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Çalışan bilgilerini günceller.</summary>
        /// <remarks>
        /// **Abone değil:** Sadece Owner güncelleyebilir. Position ve departmentId görmezden gelinir.
        ///
        /// **Abone:** Manager+ güncelleyebilir. Kendi seviyenizden yüksek role atama yapılamaz.
        /// </remarks>
        /// <param name="memberId">Güncellenecek çalışanın BusinessMember ID'si</param>
        [HttpPut("{memberId}")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto>), 400)]
        public async Task<IActionResult> UpdateMember(Guid memberId, [FromBody] UpdateBusinessMemberRequestDto requestDto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.UpdateMemberAsync(userId, memberId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Çalışanı şirketten çıkarır (soft delete).</summary>
        /// <remarks>
        /// **Abone değil:** Sadece Owner çıkarabilir.
        ///
        /// **Abone:** Manager+ çıkarabilir. Kendi seviyenizden yüksek birini çıkaramazsınız.
        /// </remarks>
        /// <param name="memberId">Çıkarılacak çalışanın BusinessMember ID'si</param>
        [HttpDelete("{memberId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> RemoveMember(Guid memberId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.RemoveMemberAsync(userId, memberId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Çalışana PDF belge yükler.</summary>
        /// <remarks>**Abone değil:** Sadece Owner. **Abone:** Manager+</remarks>
        /// <param name="memberId">Belge yüklenecek çalışanın BusinessMember ID'si</param>
        [HttpPost("{memberId}/documents")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>), 400)]
        public async Task<IActionResult> UploadDocument(Guid memberId, [FromForm] UploadDocumentRequestDto requestDto)
        {
            if (requestDto.File == null || requestDto.File.Length == 0)
                return BadRequest(ServiceResponse<object>.ErrorResult("Lütfen bir dosya seçiniz."));

            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _memberService.UploadDocumentAsync(userId, memberId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Çalışana ait belgeyi günceller.</summary>
        /// <remarks>**Abone değil:** Sadece Owner. **Abone:** Manager+</remarks>
        /// <param name="documentId">Güncellenecek belge ID'si</param>
        [HttpPut("documents/{documentId}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>), 400)]
        public async Task<IActionResult> UpdateDocument(Guid documentId, [FromForm] UpdateDocumentRequestDto requestDto)
        {
            if (string.IsNullOrWhiteSpace(requestDto.DocumentType) && requestDto.File == null)
                return BadRequest(ServiceResponse<object>.ErrorResult("Güncellenecek bir bilgi (Belge Tipi veya Dosya) göndermelisiniz."));

            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _memberService.UpdateDocumentAsync(userId, documentId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Çalışana ait belgeyi siler. Manager+ yetkisi gerekir.</summary>
        /// <param name="documentId">Silinecek belge ID'si</param>
        /// <summary>Çalışan belgesini siler.</summary>
        /// <remarks>**Abone değil:** Sadece Owner. **Abone:** Manager+</remarks>
        /// <param name="documentId">Silinecek belge ID'si</param>
        [HttpDelete("documents/{documentId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.DeleteDocumentAsync(userId, documentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Çalışan belgesini indirir.</summary>
        /// <remarks>Kendi belgesi veya: **Abone değil:** Owner. **Abone:** Manager+</remarks>
        /// <param name="documentId">İndirilecek belge ID'si</param>
        [HttpGet("documents/{documentId}/download")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> GetDocument(Guid documentId)
        {
            var currentUserId = GetUserId();
            if (currentUserId == Guid.Empty) return Unauthorized();

            var result = await _memberService.GetDocumentFileAsync(currentUserId, documentId);
            if (!result.Success) return BadRequest(result.Message);

            return File(result.Data!.FileBytes, result.Data.ContentType, result.Data.FileName);
        }

        /// <summary>
        /// Mevcut kullanıcıyı şirkete doğrudan ekler. Manager+ yetkisi gerekir.
        /// Yeni kullanıcı ise hesap oluşturulup geçici şifre e-posta ile gönderilir.
        /// </summary>
        /// <remarks>
        /// Şirket **abone değilse**: position görmezden gelinir, çalışan "Diğer" pozisyonuyla eklenir.
        /// Şirket **abone ise**: position ve departmentId geçerli olmalıdır.
        /// </remarks>
        [HttpPost("add")]
        [ProducesResponseType(typeof(ServiceResponse<Guid>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<Guid>), 400)]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeRequestDto requestDto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.AddEmployeeDirectlyAsync(userId, requestDto);
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
