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

            return Ok(new { Message = "Login successful" });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return Ok(new { Message = "Logout successful" });
        }

        private void SetJwtCookie(string token)
        {
            var cookieOptions = GetCookieOptions();
            Response.Cookies.Append("jwt", token, cookieOptions);
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