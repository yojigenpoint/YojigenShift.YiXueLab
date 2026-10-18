using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YojigenShift.YiXueLab.DAL.Entities
{
    /// <summary>
    /// 公司级标准实体基类：统一包含主键、创建时间、修改时间与软删除标记
    /// </summary>
    public abstract class BaseEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("modified_at")]
        public DateTime? ModifiedAt { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;
    }
}
