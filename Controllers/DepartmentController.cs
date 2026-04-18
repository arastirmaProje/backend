using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Department;
using Personelim.Helpers;
using Personelim.Services.Department;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// Departman yönetimi. Sadece abone şirketlerde kullanılabilir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        /// <summary>Şirkete ait aktif departmanları listeler.</summary>
        /// <remarks>Şirket abone değilse hata döner. Her departmanın hangi kategoriye ait olduğu ve kaç üyesi olduğu görünür.</remarks>
        /// <param name="businessId">Şirket ID'si</param>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(ServiceResponse<List<DepartmentResponseDto>>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<List<DepartmentResponseDto>>), 400)]
        public async Task<IActionResult> GetDepartments(Guid businessId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _departmentService.GetDepartmentsByBusinessIdAsync(userId, businessId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Yeni departman oluşturur. Manager+ yetkisi ve abonelik gerekir.</summary>
        /// <remarks>
        /// <c>category</c> alanı için geçerli değerleri <c>GET /api/job-titles/categories</c> endpoint'inden alabilirsiniz.
        /// Departmana atanan kategori, o departmandaki çalışanlara önerilecek unvan listesini belirler.
        ///
        /// Örnek:
        /// - category: "Yazılım &amp; Teknoloji" → çalışanlara Senior Developer, DevOps Engineer vb. unvanlar önerilir
        /// - category: "Satış &amp; Pazarlama" → Satış Temsilcisi, SEO Uzmanı vb. unvanlar önerilir
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceResponse<DepartmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<DepartmentResponseDto>), 400)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _departmentService.CreateDepartmentAsync(userId, requestDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Departman adını veya kategorisini günceller. Manager+ yetkisi gerekir.</summary>
        /// <param name="departmentId">Güncellenecek departman ID'si</param>
        [HttpPut("{departmentId}")]
        [ProducesResponseType(typeof(ServiceResponse<DepartmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<DepartmentResponseDto>), 400)]
        public async Task<IActionResult> UpdateDepartment(Guid departmentId, [FromBody] UpdateDepartmentRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _departmentService.UpdateDepartmentAsync(userId, departmentId, requestDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Departmanı siler (soft delete). Manager+ yetkisi gerekir.</summary>
        /// <remarks>Departmandaki çalışanların departman bilgisi null yapılır, çalışanlar silinmez.</remarks>
        /// <param name="departmentId">Silinecek departman ID'si</param>
        [HttpDelete("{departmentId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> DeleteDepartment(Guid departmentId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _departmentService.DeleteDepartmentAsync(userId, departmentId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserIdFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "uid" || c.Type == "id" || c.Type == ClaimTypes.NameIdentifier);
            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }
}
