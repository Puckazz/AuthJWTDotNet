using api.Entities;

namespace api.Dtos
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public User? User { get; set; }

        public static AuthResult CreateSuccess(string token, User user)
        {
            return new AuthResult
            {
                Success = true,
                Token = token,
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
