using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Services.Business
{
    public interface IBusinessService
    {
        Task<ServiceResponse<BusinessDocumentResponseDto>> UploadBusinessDocumentAsync(Guid userId, Guid businessId, UploadBusinessDocumentRequestDto requestDto);
         Task<ServiceResponse<List<BusinessDocumentResponseDto>>> GetBusinessDocumentsAsync(Guid userId, Guid businessId);
         Task<ServiceResponse<bool>> DeleteBusinessDocumentAsync(Guid userId, Guid documentId);
        Task<ServiceResponse<List<BusinessResponseDto>>> GetAllBusinessesAsync(Guid? userId);
        Task<ServiceResponse<bool>> VerifyBusinessAsync(Guid userId, VerifyBusinessRequestDto requestDto);
        Task<ServiceResponse<BusinessResponseDto>> CreateBusinessAsync(CreateBusinessRequestDto requestDto, Guid userId);
        Task<ServiceResponse<BusinessResponseDto>> GetBusinessByIdAsync(Guid? userId, Guid businessId);
        Task<ServiceResponse<BusinessResponseDto>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequestDto requestDto);
    }
}