using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.Data;
using Personelim.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// İş unvanı (pozisyon) referans verileri. Çalışan ekleme/güncelleme formlarında kullanılır.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class JobTitlesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobTitlesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Tüm iş unvanlarını düz liste olarak döner.</summary>
        /// <remarks>Her unvanın <c>name</c>, <c>role</c> (yetki seviyesi) ve <c>category</c> (sektör) bilgisi bulunur.</remarks>
        [HttpGet]
        public IActionResult GetAll()
        {
            var result = JobTitles.All.Select(j => new
            {
                id       = j.Id,
                name     = j.Name,
                role     = j.Role.ToString(),
                category = j.Category
            });
            return Ok(result);
        }

        /// <summary>İş unvanlarını sektör kategorisine göre gruplar.</summary>
        /// <remarks>
        /// Dropdown menüde gruplanmış gösterim için idealdir.
        ///
        /// Dönen rol seviyeleri: <c>Employee</c> → <c>TeamLead</c> → <c>Manager</c> → <c>CEO</c> → <c>Owner</c>
        /// </remarks>
        [HttpGet("grouped")]
        public IActionResult GetGrouped()
        {
            var result = JobTitles.All
                .GroupBy(j => j.Category)
                .Select(g => new
                {
                    categoryId   = JobTitles.GetCategoryId(g.Key),
                    categoryName = g.Key,
                    titles       = g.Select(j => new { id = j.Id, name = j.Name, role = j.Role.ToString() })
                });
            return Ok(result);
        }

        /// <summary>Mevcut sektör kategorilerini id + isim olarak listeler.</summary>
        /// <remarks>
        /// Departman oluştururken veya <c>by-category</c> çağrısında <c>categoryId</c> için bu listeden seçim yapılmalıdır.
        ///
        /// Örnek: <c>{ "id": 2, "name": "Yazılım &amp; Teknoloji" }</c>
        /// </remarks>
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var result = JobTitles.CategoryMap.Select(kv => new { id = kv.Key, name = kv.Value });
            return Ok(result);
        }

        /// <summary>Belirli bir kategoriye ait iş unvanlarını döner.</summary>
        /// <remarks>
        /// <c>categoryId</c> için önce <c>GET /api/job-titles/categories</c> çağırın.
        ///
        /// Örnek: categoryId=2 → Yazılım &amp; Teknoloji unvanları
        /// </remarks>
        /// <param name="categoryId">Kategori ID'si (categories endpoint'inden alınır)</param>
        [HttpGet("by-category/{categoryId:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult GetByCategory(int categoryId)
        {
            var categoryName = JobTitles.GetCategoryName(categoryId);
            if (categoryName == null)
                return NotFound(new { message = "Kategori bulunamadı." });

            var titles = JobTitles.GetByCategory(categoryName);
            return Ok(new
            {
                categoryId,
                categoryName,
                titles = titles.Select(j => new { id = j.Id, name = j.Name, role = j.Role.ToString() })
            });
        }

        /// <summary>Seçili departmanın kategorisine ait iş unvanlarını döner.</summary>
        /// <remarks>
        /// Çalışan eklerken departman seçildikten sonra bu endpoint çağrılarak
        /// o departmana uygun unvan listesi alınır ve rol dropdown'ı doldurulur.
        ///
        /// Örnek: "Backend Ekibi" departmanı "Yazılım &amp; Teknoloji" kategorisindeyse
        /// sadece yazılım unvanları döner.
        /// </remarks>
        /// <param name="departmentId">Seçilen departmanın ID'si</param>
        [HttpGet("by-department/{departmentId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByDepartment(Guid departmentId)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive);

            if (department == null)
                return NotFound(new { message = "Departman bulunamadı." });

            var titles = JobTitles.GetByCategory(department.Category);
            return Ok(new
            {
                categoryId   = JobTitles.GetCategoryId(department.Category),
                categoryName = department.Category,
                titles       = titles.Select(j => new { id = j.Id, name = j.Name, role = j.Role.ToString() })
            });
        }
    }
}
