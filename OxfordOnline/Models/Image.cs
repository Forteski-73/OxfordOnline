using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OxfordOnline.Models
{
    [Table("product_image")]
    public class Image
    {
        [Key]
        [Column("id")]
        public int? Id { get; set; }

        [Required]
        [Column("path")]
        public string Path { get; set; } = string.Empty;

        [Column("sequence")]
        public int? Sequence { get; set; }

        [Required]
        [Column("product_id")]
        [MaxLength(20)]
        public string ProductId { get; set; } = string.Empty;

        // Navegação para Product
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }
    }
}
