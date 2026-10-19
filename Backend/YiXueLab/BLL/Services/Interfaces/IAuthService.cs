using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;

namespace YojigenShift.YiXueLab.BLL.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterRequestDto req, CancellationToken ct = default);
        Task<ServiceResult<UserProfileDto>> ValidateCredentialsAsync(LoginRequestDto req, CancellationToken ct = default);
        Task<ServiceResult<string>> GenerateInviteCodeAsync(Guid adminUserId, CancellationToken ct = default);
    }
}
