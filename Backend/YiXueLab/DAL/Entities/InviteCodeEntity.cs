using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YojigenShift.YiXueLab.DAL.Entities
{
    /// <summary>
    /// 邀请码表 (invite_codes)
    /// </summary>
    [Table("invite_codes")]
    public class InviteCodeEntity : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Column("generated_by")]
        public Guid GeneratedByUserId { get; set; }

        [Column("is_used")]
        public bool IsUsed { get; set; } = false;

        [Column("used_by")]
        public Guid? UsedByUserId { get; set; }

        // 导航属性
        public UserEntity Generator { get; set; } = null!;
        public UserEntity? UsedByUser { get; set; }
    }
}
