using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Task;
using Personelim.Services.Task;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bu controller'daki tüm işlemler için giriş yapmış olmak gerekir
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // 1. Yeni Görev Oluşturma
        // POST: api/task/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.CreateTaskAsync(userId, request);

            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        
        [HttpGet("business/{businessId}")]
        public async Task<IActionResult> GetTasksByBusiness(Guid businessId)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.GetTasksByBusinessAsync(userId, businessId);

            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        
        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.GetMyTasksAsync(userId);

            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        
        [HttpPut("{taskId}/status")]
        public async Task<IActionResult> UpdateStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.UpdateTaskStatusAsync(userId, taskId, request);

            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask(Guid taskId)
        {
            var userId = GetCurrentUserId();
            var response = await _taskService.DeleteTaskAsync(userId, taskId);

            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("Kullanıcı kimliği doğrulanamadı.");
            }
            return Guid.Parse(userIdClaim);
        }
    }
}