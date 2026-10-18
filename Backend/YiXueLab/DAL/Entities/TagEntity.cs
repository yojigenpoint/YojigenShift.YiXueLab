using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YojigenShift.YiXueLab.DAL.Entities
{
    /// <summary>
    /// 标签字典表 (tags)
    /// </summary>
    [Table("tags")]
    public class TagEntity : BaseEntity
    {
        [Required]
        [MaxLength(30)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("category")]
        public string? Category { get; set; }

        public ICollection<CaseTagEntity> CaseTags { get; set; } = new List<CaseTagEntity>();
    }
}
