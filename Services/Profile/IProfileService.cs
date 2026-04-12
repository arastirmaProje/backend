using Personelim.DTOs.Auth;
using Personelim.Helpers;

public interface IProfileService
{
    Task<ServiceResponse<UserProfileResponseDto>> GetUserProfileAsync(Guid userId);
    Task<ServiceResponse<UserProfileResponseDto>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequestDto requestDto);
    Task<ServiceResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto requestDto);
    Task<ServiceResponse<bool>> DeleteUserAsync(Guid userId);
}