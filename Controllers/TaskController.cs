using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Task;
using Personelim.Helpers;
using Personelim.Services.Task;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// Görev yönetimi. Oluşturma, listeleme, durum güncelleme ve silme işlemleri.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>Yeni görev oluşturur ve çalışanlara atar.</summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(ServiceResponse<object>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<object>), 400)]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequestDto requestDto)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.CreateTaskAsync(userId, requestDto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>Şirketteki tüm görevleri listeler.</summary>
        /// <param name="businessId">Şirket ID'si</param>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetTasksByBusiness(Guid businessId)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.GetTasksByBusinessAsync(userId, businessId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>Giriş yapan kullanıcıya atanmış görevleri listeler.</summary>
        [HttpGet("my-tasks")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMyTasks()
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.GetMyTasksAsync(userId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>Görev durumunu günceller (örn: Tamamlandı, Devam Ediyor).</summary>
        /// <param name="taskId">Güncellenecek görev ID'si</param>
        [HttpPut("{taskId}/status")]
        [ProducesResponseType(typeof(ServiceResponse<object>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<object>), 400)]
        public async Task<IActionResult> UpdateStatus(Guid taskId, [FromBody] UpdateTaskStatusRequestDto requestDto)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.UpdateTaskStatusAsync(userId, taskId, requestDto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>Görevi siler.</summary>
        /// <remarks>**Abone değil:** Sadece Owner. **Abone:** Manager+</remarks>
        /// <param name="taskId">Silinecek görev ID'si</param>
        [HttpDelete("{taskId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> DeleteTask(Guid taskId)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.DeleteTaskAsync(userId, taskId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Kullanıcı kimliği doğrulanamadı.");
            return Guid.Parse(userIdClaim);
        }
    }
}
