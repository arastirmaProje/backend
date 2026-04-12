using Microsoft.AspNetCore.Identity.Data;
using Personelim.DTOs.Auth;
using Personelim.Helpers;


namespace Personelim.Services.Auth
{
    public interface IAuthService
    {
        Task<ServiceResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto requestDto);
        Task<ServiceResponse<AuthResponseDto>> LoginAsync(LoginRequestDto requestDto);
      
        Task<ServiceResponse<bool>> LogoutAsync(Guid userId);
        Task<ServiceResponse<ForgotPasswordResponse>> ForgotPasswordAsync(Personelim.DTOs.Auth.ForgotPasswordRequest request);
        Task<ServiceResponse<bool>> VerifyResetCodeAsync(VerifyResetCodeRequest request);
        Task<ServiceResponse<bool>> ResetPasswordAsync(Personelim.DTOs.Auth.ResetPasswordRequest request);
    }
}