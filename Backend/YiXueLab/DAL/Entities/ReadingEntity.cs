using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YojigenShift.YiXueLab.DAL.Entities
{
    /// <summary>
    /// 断语记录表 (readings)
    /// </summary>
    [Table("readings")]
    public class ReadingEntity : BaseEntity
    {
        [Column("case_id")]
        public Guid CaseId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("likes_count")]
        public int LikesCount { get; set; } = 0;

        // 导航属性
        public DivinationCaseEntity Case { get; set; } = null!;
        public UserEntity User { get; set; } = null!;
    }
}
