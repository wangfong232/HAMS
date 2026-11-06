using System;
using System.ComponentModel.DataAnnotations;

namespace HAMS.Domain.Common
{
    public abstract class BaseEntity : IAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
