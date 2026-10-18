using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.DAL.Entities
{
    /// <summary>
    /// 案例/演武场表 (cases)
    /// </summary>
    [Table("cases")]
    public class DivinationCaseEntity : BaseEntity
    {
        [Column("publisher_id")]
        public Guid PublisherId { get; set; }

        [Column("divination_type")]
        public DivinationType DivinationType { get; set; } = DivinationType.Bazi;

        [Required]
        [Column("divination_data", TypeName = "jsonb")]
        public DivinationPayloadBase DivinationData { get; set; } = null!;

        [Required]
        [Column("background_info")]
        public string BackgroundInfo { get; set; } = string.Empty;

        [Required]
        [Column("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [Column("real_feedback")]
        public string? RealFeedback { get; set; }

        [Column("status")]
        public CaseStatus Status { get; set; } = CaseStatus.Blind;

        [Column("reveal_time")]
        public DateTime RevealTime { get; set; }

        // 导航属性
        public UserEntity Publisher { get; set; } = null!;
        public ICollection<ReadingEntity> Readings { get; set; } = new List<ReadingEntity>();
        public ICollection<CaseTagEntity> CaseTags { get; set; } = new List<CaseTagEntity>();
    }
}
