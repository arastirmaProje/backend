using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Task;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums; 

namespace Personelim.Services.Task
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

      
        public async Task<ServiceResponse<TaskResponse>> CreateTaskAsync(Guid currentUserId, CreateTaskRequest request)
        {
            try
            {
               
                var isEmployee = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == request.AssignedToUserId &&
                    bm.BusinessId == request.BusinessId &&
                    bm.IsActive);

                if (!isEmployee)
                {
                    return ServiceResponse<TaskResponse>.ErrorResult("Seçilen personel bu işletmede aktif değil.");
                }
                
                if (request.EndDate < request.StartDate)
                {
                    return ServiceResponse<TaskResponse>.ErrorResult("Bitiş tarihi başlangıç tarihinden önce olamaz.");
                }

              
               
                var newTask = new TaskItem
                {
                    BusinessId = request.BusinessId,
                    AssignedByUserId = currentUserId,
                    AssignedToUserId = request.AssignedToUserId,
                    Title = request.Title,
                    Description = request.Description,
                    
                    Status = request.EndDate < DateTime.UtcNow ? "Süresi Geçti" : "Beklemede",
                    
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.TaskItems.AddAsync(newTask);
                await _context.SaveChangesAsync();

                return await GetTaskByIdInternal(newTask.Id);
            }
            catch (DbUpdateException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return ServiceResponse<TaskResponse>.ErrorResult("Görev oluşturulurken hata oluştu.", detail);
            }
            catch (Exception ex)
            {
                return ServiceResponse<TaskResponse>.ErrorResult("Görev oluşturulurken hata oluştu.", ex.Message);
            }
        }
        
        public async Task<ServiceResponse<List<TaskResponse>>> GetTasksByBusinessAsync(Guid currentUserId, Guid businessId)
        {
            try
            {
                var tasks = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .Where(t => t.BusinessId == businessId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync(); 

              
                var responseList = tasks.Select(t => MapToResponse(t)).ToList();

                return ServiceResponse<List<TaskResponse>>.SuccessResult(responseList);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<TaskResponse>>.ErrorResult("Görevler listelenirken hata oluştu.", ex.Message);
            }
        }
        
        public async Task<ServiceResponse<List<TaskResponse>>> GetMyTasksAsync(Guid currentUserId)
        {
            try
            {
                var tasks = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .Where(t => t.AssignedToUserId == currentUserId)
                    .OrderBy(t => t.EndDate)
                    .ToListAsync(); 

              
                var responseList = tasks.Select(t => MapToResponse(t)).ToList();

                return ServiceResponse<List<TaskResponse>>.SuccessResult(responseList);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<TaskResponse>>.ErrorResult("Görevleriniz alınırken hata oluştu.", ex.Message);
            }
        }
        
        public async Task<ServiceResponse<TaskResponse>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, UpdateTaskStatusRequest request)
        {
            try
            {
                var task = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null) return ServiceResponse<TaskResponse>.ErrorResult("Görev bulunamadı.");

                bool isAssignee = task.AssignedToUserId == currentUserId;
                bool isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == task.BusinessId &&
                    bm.Role == UserRole.Owner &&
                    bm.IsActive);

                if (!isAssignee && !isOwner)
                {
                    return ServiceResponse<TaskResponse>.ErrorResult("Bu görevi güncelleme yetkiniz yok.");
                }
                
                task.Status = request.Status;

                if (!string.IsNullOrEmpty(request.Thoughts))
                {
                    task.Thoughts = request.Thoughts;
                }
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                return ServiceResponse<TaskResponse>.SuccessResult(MapToResponse(task), "Görev güncellendi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<TaskResponse>.ErrorResult("Güncelleme hatası.", ex.Message);
            }
        }

       
        public async Task<ServiceResponse<bool>> DeleteTaskAsync(Guid currentUserId, Guid taskId)
        {
            try
            {
                var task = await _context.TaskItems.FindAsync(taskId);
                if (task == null) return ServiceResponse<bool>.ErrorResult("Görev bulunamadı.");

                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == task.BusinessId &&
                    bm.Role == UserRole.Owner &&
                    bm.IsActive);

                if (!isOwner) return ServiceResponse<bool>.ErrorResult("Görevi silme yetkiniz yok.");

                _context.TaskItems.Remove(task);
                await _context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResult(true, "Görev silindi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult("Silme işleminde hata.", ex.Message);
            }
        }

        private static TaskResponse MapToResponse(TaskItem t)
        {
            bool isTimeUp = t.EndDate < DateTime.UtcNow;

            string currentStatus = t.Status?.ToString() ?? "Beklemede";

            bool isCompleted =
                currentStatus.Equals("Tamamlandı", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase);

            if (isTimeUp && !isCompleted)
            {
                currentStatus = "Süresi Geçti";
            }

            return new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                AssignedToName = t.AssignedToUser != null
                    ? $"{t.AssignedToUser.FirstName} {t.AssignedToUser.LastName}"
                    : "Bilinmiyor",
                AssignedByName = t.AssignedByUser != null
                    ? $"{t.AssignedByUser.FirstName} {t.AssignedByUser.LastName}"
                    : "Bilinmiyor",
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = currentStatus,
                Difficulty = t.Difficulty, 
                Thoughts = t.Thoughts,
                IsOverdue = isTimeUp,
                CreatedAt = t.CreatedAt
            };
        }


        private async Task<ServiceResponse<TaskResponse>> GetTaskByIdInternal(Guid taskId)
        {
            var task = await _context.TaskItems
                   .Include(t => t.AssignedToUser)
                   .Include(t => t.AssignedByUser)
                   .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return ServiceResponse<TaskResponse>.ErrorResult("Hata oluştu");

            return ServiceResponse<TaskResponse>.SuccessResult(MapToResponse(task), "Görev oluşturuldu.");
        }
    }
}