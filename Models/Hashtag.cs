using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace jool_backend.Models
{
    public class Hashtag
    {
        [Key]
        public int hashtag_id { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string name { get; set; } = string.Empty;
        public int used_count { get; set; } = 0;
    }
}
