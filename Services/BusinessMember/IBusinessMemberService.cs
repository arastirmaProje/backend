using Personelim.DTOs.BusinessMember;
using Personelim.Helpers;

namespace Personelim.Services.BusinessMember
{
    public interface IBusinessMemberService
    {
        Task<ServiceResponse<List<BusinessMemberResponseDto>>> GetMembersByBusinessIdAsync(Guid currentUserId, Guid businessId);
        
        Task<ServiceResponse<BusinessMemberResponseDto>> GetMemberByIdAsync(Guid currentUserId, Guid memberId);
        
        Task<ServiceResponse<BusinessMemberResponseDto>> UpdateMemberAsync(Guid currentUserId, Guid memberId, UpdateBusinessMemberRequestDto requestDto);
        
        Task<ServiceResponse<bool>> RemoveMemberAsync(Guid currentUserId, Guid memberId);
        Task<ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>> UploadDocumentAsync(Guid currentUserId, Guid memberId, UploadDocumentRequestDto requestDto);
        Task<ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>> UpdateDocumentAsync(Guid currentUserId, Guid documentId, UpdateDocumentRequestDto requestDto);
        Task<ServiceResponse<bool>> DeleteDocumentAsync(Guid currentUserId, Guid documentId);
        Task<ServiceResponse<DocumentDownloadResponseDto>> GetDocumentFileAsync(Guid currentUserId, Guid documentId);
    }
}