using api.Entities;

namespace api.Dtos
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public User? User { get; set; }

        public static AuthResult CreateSuccess(string token, string refreshToken, User user)
        {
            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                User = user
            };
        }

        public static AuthResult Failure(string message)
        {
            return new AuthResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
