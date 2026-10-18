using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Repositories.Base;

namespace YojigenShift.YiXueLab.DAL.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<UserEntity>
    {
        Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<InviteCodeEntity?> GetValidInviteCodeAsync(string code, CancellationToken ct = default);
        Task AddInviteCodeAsync(InviteCodeEntity inviteCode, CancellationToken ct = default);
    }
}
