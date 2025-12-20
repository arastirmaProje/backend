using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Services.Business
{
    public interface IBusinessService
    {
        Task<ServiceResponse<BusinessDocumentResponse>> UploadBusinessDocumentAsync(Guid userId, Guid businessId, UploadBusinessDocumentRequest request);
         Task<ServiceResponse<List<BusinessDocumentResponse>>> GetBusinessDocumentsAsync(Guid userId, Guid businessId);
         Task<ServiceResponse<bool>> DeleteBusinessDocumentAsync(Guid userId, Guid documentId);
        Task<ServiceResponse<List<BusinessResponse>>> GetAllBusinessesAsync(Guid? userId);
        Task<ServiceResponse<bool>> VerifyBusinessAsync(Guid userId, VerifyBusinessRequest request);
        Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(CreateBusinessRequest request, Guid userId);
        Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid? userId, Guid businessId);
        Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequest request);
    }
}