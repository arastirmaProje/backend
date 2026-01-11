using Personelim.DTOs.Invitation;
using Personelim.Helpers;

namespace Personelim.Services.Invitation
{
    public interface IInvitationService
    {
        Task<ServiceResponse<InvitationResponseDto>> SendInvitationAsync(Guid userId, SendInvitationRequestDto requestDto);
        Task<ServiceResponse<string>> AcceptInvitationAsync(Guid userId, string invitationCode);
        Task<ServiceResponse<List<InvitationResponseDto>>> GetUserInvitationsAsync(string email);
        Task<ServiceResponse<string>> CancelInvitationAsync(Guid userId, Guid invitationId);
    }
}