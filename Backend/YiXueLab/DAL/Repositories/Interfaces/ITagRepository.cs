using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Repositories.Base;

namespace YojigenShift.YiXueLab.DAL.Repositories.Interfaces
{
    public interface ITagRepository : IRepository<TagEntity>
    {
        Task<List<TagEntity>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, string? defaultCategory = null, CancellationToken ct = default);
    }
}
