using Auth.API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Data
{
    public class AuthSeedData
    {
        private readonly ILogger<AuthSeedData> _logger;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthSeedData(
            ILogger<AuthSeedData> logger,
            IPasswordHasher<User> passwordHasher)
        {
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync(AuthDbContext dbContext, CancellationToken cancellationToken = default)
        {
            // Kiểm tra lại xem có dữ liệu chưa
            if (await dbContext.Users.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Dữ liệu người dùng đã tồn tại. Bỏ qua quá trình seed data.");
                return;
            }

            _logger.LogInformation("Bắt đầu khởi tạo dữ liệu người dùng...");

            // Tạo dữ liệu người dùng admin
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@example.com",
                Roles = new List<string> { "Admin" }
            };
            adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, "Admin@123");

            // Tạo người dùng thường
            var regularUser = new User
            {
                Username = "user",
                Email = "user@example.com",
                Roles = new List<string> { "User" }
            };
            regularUser.PasswordHash = _passwordHasher.HashPassword(regularUser, "User@123");

            await dbContext.Users.AddRangeAsync(new[] { adminUser, regularUser }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã khởi tạo xong dữ liệu người dùng.");
        }
    }
}