using Personelim.DTOs.Task;
using Personelim.Helpers; // ServiceResponse için
using Personelim.Models.Enums;

namespace Personelim.Services.Task
{
    public interface ITaskService
    {
        // Görev Oluştur (İşletme Sahibi)
        System.Threading.Tasks.Task<ServiceResponse<TaskResponse>> CreateTaskAsync(Guid currentUserId, CreateTaskRequest request);
        
        // İşletmeye ait tüm görevleri getir (İşletme Sahibi için)
        System.Threading.Tasks.Task<ServiceResponse<List<TaskResponse>>> GetTasksByBusinessAsync(Guid currentUserId, Guid businessId);
        
        // Bana atanan görevleri getir (Personel için)
        System.Threading.Tasks.Task<ServiceResponse<List<TaskResponse>>> GetMyTasksAsync(Guid currentUserId);
        
        // Görev detayını/durumunu güncelle (Personel veya Sahip)
        System.Threading.Tasks.Task<ServiceResponse<TaskResponse>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, UpdateTaskStatusRequest request);

        // Görevi Sil
        System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteTaskAsync(Guid currentUserId, Guid taskId);
    }
}