using Auth.API.DTOs;
using Auth.API.Entities;

namespace Auth.API.Interfaces.Services
{
    public interface IUserService
    {
        Task<User> ValidateUserAsync(string username, string password);
        Task<User> GetUserByUsernameAsync(string username);
        Task<bool> RegisterUserAsync(RegisterUserRequest request);
    }
}
