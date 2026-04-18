using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Task;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Resources;
using Personelim.Helpers;
using Personelim.Services.Slack;

namespace Personelim.Services.Task
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ISlackService _slackService;

        public TaskService(AppDbContext context, IStringLocalizer<SharedResource> localizer, ISlackService slackService)
        {
            _context = context;
            _localizer = localizer;
            _slackService = slackService;
        }

        public async Task<ServiceResponse<TaskResponseDto>> CreateTaskAsync(Guid currentUserId, CreateTaskRequestDto requestDto)
        {
            try
            {
                var isEmployee = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == requestDto.AssignedToUserId &&
                    bm.BusinessId == requestDto.BusinessId &&
                    bm.IsActive);
                
                if (!isEmployee)
                    return ServiceResponse<TaskResponseDto>.ErrorResult(_localizer["PersonnelNotActiveInBusiness"]);
                
                if (requestDto.EndDate < requestDto.StartDate)
                    return ServiceResponse<TaskResponseDto>.ErrorResult(_localizer["StartDateAfterEndDate"]);

                var newTask = new TaskItem
                {
                    BusinessId = requestDto.BusinessId,
                    AssignedByUserId = currentUserId,
                    AssignedToUserId = requestDto.AssignedToUserId,
                    Title = requestDto.Title,
                    Description = requestDto.Description,
                    Status = requestDto.EndDate < DateTime.UtcNow ? _localizer["StatusOverdue"] : _localizer["StatusPending"],
                    StartDate = requestDto.StartDate,
                    EndDate = requestDto.EndDate,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.TaskItems.AddAsync(newTask);
                await _context.SaveChangesAsync();

                var result = await GetTaskByIdInternal(newTask.Id);
                if (result.Success && result.Data != null)
                {
                    await _slackService.SendAsync(requestDto.BusinessId, SlackEventTypes.TaskCreated, new
                    {
                        blocks = new object[]
                        {
                            new { type = "header", text = new { type = "plain_text", text = "📋 Yeni Görev Oluşturuldu", emoji = true } },
                            new { type = "section", fields = new[] {
                                new { type = "mrkdwn", text = $"*Başlık:*\n{result.Data.Title}" },
                                new { type = "mrkdwn", text = $"*Atayan:*\n{result.Data.AssignedByName}" }
                            }},
                            new { type = "section", fields = new[] {
                                new { type = "mrkdwn", text = $"*Atanan:*\n{result.Data.AssignedToName}" },
                                new { type = "mrkdwn", text = $"*Durum:*\nBeklemede" }
                            }},
                            new { type = "section", fields = new[] {
                                new { type = "mrkdwn", text = $"*Başlangıç:*\n{result.Data.StartDate:dd.MM.yyyy}" },
                                new { type = "mrkdwn", text = $"*Bitiş:*\n{result.Data.EndDate:dd.MM.yyyy}" }
                            }},
                            new { type = "section", text = new { type = "mrkdwn", text = $"*Açıklama:*\n{result.Data.Description}" } },
                            new { type = "divider" }
                        }
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                var detail = ex is DbUpdateException dbEx ? (dbEx.InnerException?.Message ?? dbEx.Message) : ex.Message;
                return ServiceResponse<TaskResponseDto>.ErrorResult(_localizer["TaskCreateError"], detail);
            }
        }

        public async Task<ServiceResponse<List<TaskResponseDto>>> GetTasksByBusinessAsync(Guid currentUserId, Guid businessId)
        {
            try
            {
                var tasks = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .Where(t => t.BusinessId == businessId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var responseList = tasks.Select(MapToResponse).ToList();
                return ServiceResponse<List<TaskResponseDto>>.SuccessResult(responseList);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<TaskResponseDto>>.ErrorResult(_localizer["TasksFetchError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<List<TaskResponseDto>>> GetMyTasksAsync(Guid currentUserId)
        {
            try
            {
                var tasks = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .Where(t => t.AssignedToUserId == currentUserId)
                    .OrderBy(t => t.EndDate)
                    .ToListAsync();

                var responseList = tasks.Select(MapToResponse).ToList();
                return ServiceResponse<List<TaskResponseDto>>.SuccessResult(responseList);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<TaskResponseDto>>.ErrorResult(_localizer["MyTasksFetchError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<TaskResponseDto>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, UpdateTaskStatusRequestDto requestDto)
        {
            try
            {
                var task = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .FirstOrDefaultAsync(t => t.Id == taskId);
                
                if (task == null) return ServiceResponse<TaskResponseDto>.ErrorResult(_localizer["TaskNotFound"]);

                task.Status = requestDto.Status;
                task.Difficulty = requestDto.Difficulty?.ToLowerInvariant();
                
                if (!string.IsNullOrEmpty(requestDto.Thoughts)) task.Thoughts = requestDto.Thoughts;
                
                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResponse<TaskResponseDto>.SuccessResult(MapToResponse(task), _localizer["TaskUpdated"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<TaskResponseDto>.ErrorResult(_localizer["TaskUpdateError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteTaskAsync(Guid currentUserId, Guid taskId)
        {
            try
            {
                var task = await _context.TaskItems.FindAsync(taskId);
                if (task == null) return ServiceResponse<bool>.ErrorResult(_localizer["TaskNotFound"]);

                var taskBusiness = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == task.BusinessId);
                var allowedForTask = JobTitles.EffectiveManagementPositions(taskBusiness?.IsSubscribed ?? false);
                var isManager = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == task.BusinessId &&
                    allowedForTask.Contains(bm.Position) &&
                    bm.IsActive);

                if (!isManager) return ServiceResponse<bool>.ErrorResult(_localizer["NoPermissionDeleteTask"]);

                _context.TaskItems.Remove(task);
                await _context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResult(true, _localizer["TaskDeleted"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["TaskDeleteError"], ex.Message);
            }
        }

        private TaskResponseDto MapToResponse(TaskItem t)
        {
            bool isTimeUp = t.EndDate < DateTime.UtcNow;
            string currentStatus = t.Status ?? _localizer["StatusPending"];
            
            bool isCompleted = currentStatus.Equals(_localizer["StatusCompleted"], StringComparison.OrdinalIgnoreCase) ||
                               currentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase);

            if (isTimeUp && !isCompleted)
            {
                currentStatus = _localizer["StatusOverdue"];
            }

            return new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                AssignedToName = t.AssignedToUser != null ? $"{t.AssignedToUser.FirstName} {t.AssignedToUser.LastName}" : _localizer["UnknownUser"],
                AssignedByName = t.AssignedByUser != null ? $"{t.AssignedByUser.FirstName} {t.AssignedByUser.LastName}" : _localizer["UnknownUser"],
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = currentStatus,
                Difficulty = t.Difficulty,
                Thoughts = t.Thoughts,
                IsOverdue = isTimeUp,
                CreatedAt = t.CreatedAt
            };
        }

        private async Task<ServiceResponse<TaskResponseDto>> GetTaskByIdInternal(Guid taskId)
        {
            var task = await _context.TaskItems
                   .Include(t => t.AssignedToUser)
                   .Include(t => t.AssignedByUser)
                   .FirstOrDefaultAsync(t => t.Id == taskId);
            
            if (task == null) return ServiceResponse<TaskResponseDto>.ErrorResult(_localizer["GeneralError"]);
            return ServiceResponse<TaskResponseDto>.SuccessResult(MapToResponse(task), _localizer["TaskCreated"]);
        }
    }
}