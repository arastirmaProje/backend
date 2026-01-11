using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using Personelim.Services.Auth;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ServiceResponse<AuthResponseDto>>> Register(RegisterRequestDto requestDto)
        {
            var result = await _authService.RegisterAsync(requestDto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ServiceResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto requestDto)
        {
            var result = await _authService.LoginAsync(requestDto);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        
        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<ServiceResponse<bool>>> Logout()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _authService.LogoutAsync(userId);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("verify-reset-code")]
        public async Task<ActionResult<ServiceResponse<bool>>> VerifyResetCode(
            [FromBody] VerifyResetCodeRequest request)
        {
            var result = await _authService.VerifyResetCodeAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ServiceResponse<bool>>> ResetPassword(
            [FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}