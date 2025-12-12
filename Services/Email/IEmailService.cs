using System.Threading.Tasks;

namespace Personelim.Services.Email
{
    public interface IEmailService
    {
        Task<bool> SendInvitationEmailAsync(string email, string invitationCode, string businessName, string inviterName, string message);
        
        Task<bool> SendPasswordResetCodeAsync(string email, string code, string userName);
        
        Task<bool> SendAccountCreatedEmailAsync(string email, string firstName, string plainPassword, string businessName = null);
        
        Task<bool> SendAddedToBusinessEmailAsync(string email, string firstName, string businessName);
        Task<bool> SendBusinessVerificationCodeAsync(string email, string userName, string businessName, string code);
        
    } 
}