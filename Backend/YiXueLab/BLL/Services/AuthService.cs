using System.Security.Cryptography;
using System.Text;
using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.UOW;

namespace YojigenShift.YiXueLab.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;

        public AuthService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterRequestDto req, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return ServiceResult<UserProfileDto>.Failure("用户名和密码不能为空。");

            if (string.IsNullOrWhiteSpace(req.WechatNickname))
                return ServiceResult<UserProfileDto>.Failure("请填写微信群昵称以便核对身份。");

            // 1. 校验邀请码是否有效
            var invite = await _uow.Users.GetValidInviteCodeAsync(req.InviteCode.Trim().ToUpperInvariant(), ct);
            if (invite == null)
                return ServiceResult<UserProfileDto>.Failure("邀请码无效或已被使用。", ServiceErrorCode.ValidationError);

            // 2. 校验用户名是否已被占用
            var existingUser = await _uow.Users.GetByUsernameAsync(req.Username.Trim(), ct);
            if (existingUser != null)
                return ServiceResult<UserProfileDto>.Failure("该用户名已被注册。", ServiceErrorCode.Conflict);

            // 3. 开启显式事务：创建用户 + 核销邀请码
            await _uow.BeginTransactionAsync(ct);

            var newUser = new UserEntity
            {
                Username = req.Username.Trim(),
                PasswordHash = HashPassword(req.Password),
                WechatNickname = req.WechatNickname.Trim(),
                Role = UserRole.User,
                IsActive = true
            };

            await _uow.Users.AddAsync(newUser, ct);

            invite.IsUsed = true;
            invite.UsedByUserId = newUser.Id;

            await _uow.CommitTransactionAsync(ct);

            return ServiceResult<UserProfileDto>.Success(MapToProfile(newUser));
        }

        public async Task<ServiceResult<UserProfileDto>> ValidateCredentialsAsync(LoginRequestDto req, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByUsernameAsync(req.Username.Trim(), ct);
            if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
                return ServiceResult<UserProfileDto>.Failure("用户名或密码错误。", ServiceErrorCode.Unauthorized);

            if (!user.IsActive)
                return ServiceResult<UserProfileDto>.Failure("该账号已被冻结，请联系群主。", ServiceErrorCode.Forbidden);

            return ServiceResult<UserProfileDto>.Success(MapToProfile(user));
        }

        public async Task<ServiceResult<string>> GenerateInviteCodeAsync(Guid adminUserId, CancellationToken ct = default)
        {
            var admin = await _uow.Users.GetByIdAsync(adminUserId, ct);
            if (admin == null || admin.Role != UserRole.Admin)
                return ServiceResult<string>.Failure("仅管理员有权生成邀请码。", ServiceErrorCode.Forbidden);

            // 生成形如 "YX-8F3A9C" 的易读防混淆邀请码
            string code = $"YX-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}";

            var inviteEntity = new InviteCodeEntity
            {
                Code = code,
                GeneratedByUserId = adminUserId,
                IsUsed = false
            };

            await _uow.Users.AddInviteCodeAsync(inviteEntity, ct);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<string>.Success(code);
        }

        private static UserProfileDto MapToProfile(UserEntity u)
            => new(u.Id, u.Username, u.WechatNickname, u.Role, u.CreatedAt);

        // --- PBKDF2 SHA-256 密码安全哈希与恒定时间比对 ---
        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations: 100_000,
                HashAlgorithmName.SHA256,
                outputLength: 32);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations: 100_000,
                HashAlgorithmName.SHA256,
                outputLength: 32);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
