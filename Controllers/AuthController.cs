using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using Personelim.Services.Auth;
using System.Security.Claims;

namespace Personelim.Controllers
{
    /// <summary>
    /// Kimlik doğrulama ve kullanıcı hesabı işlemleri.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Yeni kullanıcı kaydı oluşturur.</summary>
        /// <remarks>E-posta adresi sistemde kayıtlı olmamalıdır.</remarks>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), 400)]
        public async Task<ActionResult<ServiceResponse<AuthResponseDto>>> Register(RegisterRequestDto requestDto)
        {
            var result = await _authService.RegisterAsync(requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>E-posta ve şifre ile giriş yapar, JWT token döner.</summary>
        /// <remarks>
        /// Dönen token'ı tüm yetkili isteklerde <c>Authorization: Bearer {token}</c> header'ı ile gönderin.
        /// Token süresi 7 gündür.
        /// </remarks>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), 400)]
        public async Task<ActionResult<ServiceResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto requestDto)
        {
            var result = await _authService.LoginAsync(requestDto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Oturumu kapatır.</summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        public async Task<ActionResult<ServiceResponse<bool>>> Logout()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _authService.LogoutAsync(userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Şifre sıfırlama kodu gönderir.</summary>
        /// <remarks>Belirtilen e-posta adresine 6 haneli doğrulama kodu gönderilir. Kod 15 dakika geçerlidir.</remarks>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Şifre sıfırlama kodunu doğrular.</summary>
        /// <remarks>E-posta ile gelen 6 haneli kodu burada doğrulayın. Başarılı doğrulamadan sonra reset-password çağrılabilir.</remarks>
        [HttpPost("verify-reset-code")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<ActionResult<ServiceResponse<bool>>> VerifyResetCode([FromBody] VerifyResetCodeRequest request)
        {
            var result = await _authService.VerifyResetCodeAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Yeni şifre belirler.</summary>
        /// <remarks>verify-reset-code ile doğrulanmış kod kullanılarak şifre değiştirilebilir.</remarks>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 200)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), 400)]
        public async Task<ActionResult<ServiceResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}
