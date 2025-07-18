using System.Security.Claims;
using api.Dtos;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Lấy ID từ JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid token");

            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound("User not found");

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };

            return Ok(userProfile);
        }
        
        [HttpGet("check-auth")]
        public IActionResult CheckAuth()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                IsAuthenticated = true,
                Username = username,
                Role = role
            });
        }
    }
}
