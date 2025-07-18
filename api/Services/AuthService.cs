using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using api.Dtos;
using api.Entities;
using api.Interfaces;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;

namespace api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<AuthResult> RegisterAsync(RegisterDto model)
        {
            // Kiểm tra user đã tồn tại
            var userByUsername = await _userRepository.GetUserByUsernameAsync(model.Username);
            var userByEmail = await _userRepository.GetUserByEmailAsync(model.Email);

            if (userByUsername != null || userByEmail != null)
                return AuthResult.Failure("Username or email already exists.");

            // Tạo user mới
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "User"
            };

            await _userRepository.AddUserAsync(user);

            // Tạo JWT token
            var token = GenerateJwtToken(user.Id, user.Username, user.Role);

            return AuthResult.CreateSuccess(token, user);
        }

        public async Task<AuthResult> LoginAsync(LoginDto model)
        {
            var user = await _userRepository.GetUserByUsernameAsync(model.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return AuthResult.Failure("Invalid credentials");

            var token = GenerateJwtToken(user.Id, user.Username, user.Role);

            return AuthResult.CreateSuccess(token, user);
        }

        public string GenerateJwtToken(int userId, string username, string role)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured."));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                ]),
                Expires = DateTime.UtcNow.AddMinutes(GetExpiryInMinutes()),
                Issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured."),
                Audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured."),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
