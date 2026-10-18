using System.ComponentModel.DataAnnotations.Schema;

namespace YojigenShift.YiXueLab.DAL.Entities
{
    /// <summary>
    /// 案例与标签多对多关联表 (case_tags)
    /// </summary>
    [Table("case_tags")]
    public class CaseTagEntity
    {
        [Column("case_id")]
        public Guid CaseId { get; set; }

        [Column("tag_id")]
        public Guid TagId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DivinationCaseEntity Case { get; set; } = null!;
        public TagEntity Tag { get; set; } = null!;
    }
}
