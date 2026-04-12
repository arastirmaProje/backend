using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<ActionResult<ServiceResponse<UserProfileResponseDto>>> GetProfile()
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.GetUserProfileAsync(userId);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPut]
        public async Task<ActionResult<ServiceResponse<UserProfileResponseDto>>> UpdateProfile(
            [FromForm] UpdateUserProfileRequestDto requestDto) 
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.UpdateUserProfileAsync(userId, requestDto);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("change-password")]
        public async Task<ActionResult<ServiceResponse<bool>>> ChangePassword(
            [FromBody] ChangePasswordRequestDto requestDto)
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.ChangePasswordAsync(userId, requestDto);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpDelete("delete-account")]
        public async Task<ActionResult<ServiceResponse<bool>>> DeleteAccount()
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.DeleteUserAsync(userId);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        
        private Guid GetUserIdFromClaims()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claimValue, out var userId) ? userId : Guid.Empty;
        }
    }
}