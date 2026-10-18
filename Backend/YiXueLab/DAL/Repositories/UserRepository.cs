using Microsoft.EntityFrameworkCore;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Repositories.Base;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;

namespace YojigenShift.YiXueLab.DAL.Repositories
{
    public class UserRepository : RepositoryBase<UserEntity>, IUserRepository
    {
        public UserRepository(YiXueDbContext context) : base(context) { }

        public async Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            return await DbSet.AsNoTracking()
                              .FirstOrDefaultAsync(u => u.Username == username, ct);
        }

        public async Task<InviteCodeEntity?> GetValidInviteCodeAsync(string code, CancellationToken ct = default)
        {
            return await Context.InviteCodes
                                .FirstOrDefaultAsync(ic => ic.Code == code && !ic.IsUsed, ct);
        }

        public async Task AddInviteCodeAsync(InviteCodeEntity inviteCode, CancellationToken ct = default)
        {
            await Context.InviteCodes.AddAsync(inviteCode, ct);
        }
    }
}
