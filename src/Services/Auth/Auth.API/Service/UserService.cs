using Auth.API.Data;
using Auth.API.DTOs;
using Auth.API.Entities;
using Auth.API.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Service
{
    public class UserService : IUserService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(AuthDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<User> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Invalid password for user: {Username}", username);
                return null;
            }

            return user;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> RegisterUserAsync(RegisterUserRequest request)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            {
                _logger.LogWarning("Registration failed - username or email already exists: {Username}", request.Username);
                return false;
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Roles = new List<string> { "User" } // Default role
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Username}", request.Username);
            return true;
        }
    }
}
