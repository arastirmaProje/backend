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
                // 1. Yetki Kontrolü: Görevi veren kişi işletme sahibi (Owner) mi?
                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == request.BusinessId &&
                    bm.Role == UserRole.Owner && // Sadece Owner görev verebilir (veya Manager ekleyebilirsiniz)
                    bm.IsActive);

                if (!isOwner)
                {
                    return ServiceResponse<TaskResponse>.ErrorResult("Görev oluşturma yetkiniz yok.");
                }

                // 2. Personel Kontrolü: Görev verilen kişi gerçekten o işletmede mi?
                var isEmployee = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == request.AssignedToUserId &&
                    bm.BusinessId == request.BusinessId &&
                    bm.IsActive);

                if (!isEmployee)
                {
                    return ServiceResponse<TaskResponse>.ErrorResult("Seçilen personel bu işletmede aktif değil.");
                }

                // 3. Tarih Kontrolü
                if (request.EndDate < request.StartDate)
                {
                    return ServiceResponse<TaskResponse>.ErrorResult("Bitiş tarihi başlangıç tarihinden önce olamaz.");
                }

                // 4. Görev Oluşturma
                var newTask = new TaskItem
                {
                    BusinessId = request.BusinessId,
                    AssignedByUserId = currentUserId,
                    AssignedToUserId = request.AssignedToUserId,
                    Title = request.Title,
                    Description = request.Description,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Difficulty = request.Difficulty,
                    Status = Models.Enums.TaskStatus.Pending, // Varsayılan: Beklemede
                    Thoughts = "", // Başlangıçta boş
                    CreatedAt = DateTime.UtcNow
                };

                await _context.TaskItems.AddAsync(newTask);
                await _context.SaveChangesAsync();

                // 5. Response Hazırlama
                // Kullanıcı isimlerini almak için Include yapmamız veya DB'den çekmemiz lazım, 
                // ancak SaveChanges sonrası Id oluştuğu için hızlıca tekrar çekebiliriz veya 
                // eldeki verilerle dönebiliriz. Şık olması için tekrar çekip isimlerle dönelim.
                
                return await GetTaskByIdInternal(newTask.Id);
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
                // İşletme sahibi mi kontrolü
                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == businessId &&
                    bm.Role == UserRole.Owner && 
                    bm.IsActive);

                if (!isOwner) return ServiceResponse<List<TaskResponse>>.ErrorResult("Bu işletmenin görevlerini görüntüleme yetkiniz yok.");

                var tasks = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .Where(t => t.BusinessId == businessId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => MapToResponse(t))
                    .ToListAsync();

                return ServiceResponse<List<TaskResponse>>.SuccessResult(tasks);
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
                    .Where(t => t.AssignedToUserId == currentUserId) // Bana atananlar
                    .OrderBy(t => t.EndDate) // Bitiş tarihine göre sırala (Acil olan üstte)
                    .Select(t => MapToResponse(t))
                    .ToListAsync();

                return ServiceResponse<List<TaskResponse>>.SuccessResult(tasks);
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

                // Yetki: Ya görevi yapan kişi (AssignedTo) ya da görevi veren kişi (AssignedBy veya Owner) olmalı.
                bool isAssignee = task.AssignedToUserId == currentUserId;
                
                // Ayrıca işletme sahibi mi diye de bakabiliriz (Görevi başkası verse bile Owner müdahale edebilir)
                bool isOwner = await _context.BusinessMembers.AnyAsync(bm => 
                    bm.UserId == currentUserId && 
                    bm.BusinessId == task.BusinessId && 
                    bm.Role == UserRole.Owner &&
                    bm.IsActive);

                if (!isAssignee && !isOwner)
                {
                    return ServiceResponse<TaskResponse>.ErrorResult("Bu görevi güncelleme yetkiniz yok.");
                }

                // Güncelleme
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

                // Sadece Owner silebilir
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

        // Helper Metot: TaskItem -> TaskResponse Dönüştürücü
        private static TaskResponse MapToResponse(TaskItem t)
        {
            bool isOverdue = t.Status != Models.Enums.TaskStatus.Completed && t.EndDate < DateTime.UtcNow;

            return new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                AssignedToName = t.AssignedToUser != null ? $"{t.AssignedToUser.FirstName} {t.AssignedToUser.LastName}" : "Bilinmiyor",
                AssignedByName = t.AssignedByUser != null ? $"{t.AssignedByUser.FirstName} {t.AssignedByUser.LastName}" : "Bilinmiyor",
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = t.Status.ToString(),
                Difficulty = t.Difficulty.ToString(),
                Thoughts = t.Thoughts,
                IsOverdue = isOverdue,
                CreatedAt = t.CreatedAt
            };
        }

        // Helper Metot: Tekil görevi ID ile tüm detaylarıyla çekip response dönmek için
        private async Task<ServiceResponse<TaskResponse>> GetTaskByIdInternal(Guid taskId)
        {
             var task = await _context.TaskItems
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .FirstOrDefaultAsync(t => t.Id == taskId);
             
             if(task == null) return ServiceResponse<TaskResponse>.ErrorResult("Hata oluştu");
             
             return ServiceResponse<TaskResponse>.SuccessResult(MapToResponse(task), "Görev oluşturuldu.");
        }
    }
}