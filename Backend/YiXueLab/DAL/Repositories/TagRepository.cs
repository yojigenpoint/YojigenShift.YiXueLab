using Microsoft.EntityFrameworkCore;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Repositories.Base;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;

namespace YojigenShift.YiXueLab.DAL.Repositories
{
    public class TagRepository : RepositoryBase<TagEntity>, ITagRepository
    {
        public TagRepository(YiXueDbContext context) : base(context) { }

        public async Task<List<TagEntity>> GetOrCreateTagsAsync(
            IEnumerable<string> tagNames,
            string? defaultCategory = null,
            CancellationToken ct = default)
        {
            var normalizedNames = tagNames
                .Select(n => n.Trim().TrimStart('#'))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            if (normalizedNames.Count == 0)
                return new List<TagEntity>();

            var existingTags = await DbSet.Where(t => normalizedNames.Contains(t.Name))
                                          .ToListAsync(ct);

            var existingNameSet = existingTags.Select(t => t.Name).ToHashSet();

            var newTags = normalizedNames
                .Where(name => !existingNameSet.Contains(name))
                .Select(name => new TagEntity
                {
                    Name = name,
                    Category = defaultCategory
                })
                .ToList();

            if (newTags.Count > 0)
            {
                await DbSet.AddRangeAsync(newTags, ct);
                existingTags.AddRange(newTags);
            }

            return existingTags;
        }
    }
}
