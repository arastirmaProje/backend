using Personelim.DTOs.Task;
using Personelim.Helpers; // ServiceResponse için
using Personelim.Models.Enums;

namespace Personelim.Services.Task
{
    public interface ITaskService
    {
        // Görev Oluştur 
        System.Threading.Tasks.Task<ServiceResponse<TaskResponseDto>> CreateTaskAsync(Guid currentUserId, CreateTaskRequestDto requestDto);
        
        // İşletmeye ait tüm görevleri getir
        System.Threading.Tasks.Task<ServiceResponse<List<TaskResponseDto>>> GetTasksByBusinessAsync(Guid currentUserId, Guid businessId);
        
        // Bana atanan görevleri getir 
        System.Threading.Tasks.Task<ServiceResponse<List<TaskResponseDto>>> GetMyTasksAsync(Guid currentUserId);
        
        // Görev detayını/durumunu güncelle 
        System.Threading.Tasks.Task<ServiceResponse<TaskResponseDto>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, UpdateTaskStatusRequestDto requestDto);

        // Görevi Sil
        System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteTaskAsync(Guid currentUserId, Guid taskId);
    }
}