using System.ComponentModel.DataAnnotations;

namespace BeerWallWeb.API.Entities
{
    public class BaseEntity
    {
        [Key]
        public long Id { get; set; } // 雪花算法ID或自增ID

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public DateTime? UpdateTime { get; set; }

        public bool IsDeleted { get; set; } = false; // 软删除标\1]

    }
}
