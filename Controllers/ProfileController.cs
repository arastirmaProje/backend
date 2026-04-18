using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// Giriş yapan kullanıcının profil ve hesap işlemleri.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>Giriş yapan kullanıcının profil bilgilerini getirir.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ServiceResponse<UserProfileResponseDto>), 200)]
        public async Task<ActionResult<ServiceResponse<UserProfileResponseDto>>> GetProfile()
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.GetUserProfileAsync(userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Profil bilgilerini ve profil fotoğrafını günceller.</summary>
        /// <remarks>multipart/form-data ile fotoğraf yüklenebilir.</remarks>
        [HttpPut]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<UserProfileResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<UserProfileResponseDto>), 400)]
        public async Task<ActionResult<ServiceResponse<UserProfileResponseDto>>> UpdateProfile([FromForm] UpdateUserProfileRequestDto requestDto)
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.UpdateUserProfileAsync(userId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Kullanıcı şifresini değiştirir.</summary>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<ActionResult<ServiceResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequestDto requestDto)
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.ChangePasswordAsync(userId, requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Hesabı kalıcı olarak siler.</summary>
        /// <remarks>Bu işlem geri alınamaz. Kullanıcıya ait tüm veriler silinir.</remarks>
        [HttpDelete("delete-account")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<ActionResult<ServiceResponse<bool>>> DeleteAccount()
        {
            var userId = GetUserIdFromClaims();
            var result = await _profileService.DeleteUserAsync(userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        private Guid GetUserIdFromClaims()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claimValue, out var userId) ? userId : Guid.Empty;
        }
    }
}
