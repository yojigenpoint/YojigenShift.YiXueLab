using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.DAL.Entities
{
    /// <summary>
    /// 用户与鉴权表 (users)
    /// </summary>
    [Table("users")]
    public class UserEntity : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("wechat_nickname")]
        public string WechatNickname { get; set; } = string.Empty;

        [Column("role")]
        public UserRole Role { get; set; } = UserRole.User;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // 导航属性
        public ICollection<DivinationCaseEntity> PublishedCases { get; set; } = new List<DivinationCaseEntity>();
        public ICollection<ReadingEntity> Readings { get; set; } = new List<ReadingEntity>();
    }
}
