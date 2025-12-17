using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Services.Business
{
    public interface IBusinessService
    {
        // Parametreyi buraya da ekle
        Task<ServiceResponse<List<BusinessResponse>>> GetAllBusinessesAsync(Guid? userId);
        Task<ServiceResponse<bool>> VerifyBusinessAsync(Guid userId, VerifyBusinessRequest request);
        Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(CreateBusinessRequest request, Guid userId);
        Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid? userId, Guid businessId);
        Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequest request);
    }
}