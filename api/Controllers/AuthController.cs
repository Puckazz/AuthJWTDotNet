using System;
using System.Threading.Tasks;
using api.Dtos;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(model);
            
            if (!result.Success)
                return BadRequest(result.Message);

            SetJwtCookie(result.Token!);
            SetRefreshTokenCookie(result.RefreshToken!);

            return Ok(new { Message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(model);
            
            if (!result.Success)
                return Unauthorized(result.Message);

            SetJwtCookie(result.Token!);
            SetRefreshTokenCookie(result.RefreshToken!);

            return Ok(new { Message = "Login successful" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Refresh token not found");

            var result = await _authService.RefreshTokenAsync(refreshToken);
            
            if (!result.Success)
                return Unauthorized(result.Message);

            SetJwtCookie(result.Token!);
            SetRefreshTokenCookie(result.RefreshToken!);

            return Ok(new { Message = "Token refreshed successfully" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.RevokeTokenAsync(refreshToken);
            }

            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");
            
            return Ok(new { Message = "Logout successful" });
        }

        private void SetJwtCookie(string token)
        {
            var cookieOptions = GetCookieOptions();
            Response.Cookies.Append("jwt", token, cookieOptions);
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = GetRefreshTokenCookieOptions();
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        private CookieOptions GetCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(GetExpiryInMinutes())
            };
        }

        private CookieOptions GetRefreshTokenCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7) // 7 days for refresh token
            };
        }

        private double GetExpiryInMinutes()
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var expiryInMinutesValue = jwtSettings["ExpiryInMinutes"] ?? throw new InvalidOperationException("JWT ExpiryInMinutes is not configured.");
            if (!double.TryParse(expiryInMinutesValue, out var expiryInMinutes))
            {
                throw new InvalidOperationException("JWT ExpiryInMinutes must be a valid number.");
            }
            return expiryInMinutes;
        }
    }
}