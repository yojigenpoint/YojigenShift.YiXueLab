using System.Text;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.UOW;

namespace YojigenShift.YiXueLab.API.Setup
{
    public static class DbSeeder
    {
        public static async Task EnsureInitialAdminSeededAsync(IServiceProvider services, IConfiguration config)
        {
            using var scope = services.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string adminUsername = config["InitialAdmin:Username"] ?? "admin";
            var existingAdmin = await uow.Users.GetByUsernameAsync(adminUsername);
            if (existingAdmin != null)
            {
                return; // 管理员已存在，直接跳过
            }

            string adminPassword = config["InitialAdmin:Password"] ?? "YiXueLab@2026";
            string adminNickname = config["InitialAdmin:Nickname"] ?? "群主管理员";

            // 先植入一条系统引导邀请码，再通过标准 AuthService 注册管理员并提升权限
            var bootstrapAdminId = Guid.NewGuid();
            var seedAdmin = new UserEntity
            {
                Id = bootstrapAdminId,
                Username = adminUsername,
                PasswordHash = HashBootstrapPassword(adminPassword),
                WechatNickname = adminNickname,
                Role = UserRole.Admin,
                IsActive = true
            };

            await uow.Users.AddAsync(seedAdmin);
            await uow.SaveChangesAsync();
        }

        private static string HashBootstrapPassword(string password)
        {
            byte[] salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            byte[] hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations: 100_000,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                outputLength: 32);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
