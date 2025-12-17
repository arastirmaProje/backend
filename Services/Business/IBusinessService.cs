using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Services.Business
{
    public interface IBusinessService
    {
        Task<ServiceResponse<List<BusinessResponse>>> GetAllBusinessesAsync();
        Task<ServiceResponse<bool>> VerifyBusinessAsync(Guid userId, VerifyBusinessRequest request);
        Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(CreateBusinessRequest request, Guid userId);
        Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid? userId, Guid businessId);
        Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequest request);
    }
}