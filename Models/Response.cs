using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace jool_backend.Models
{
    public class Response
    {
        [Key]
        public int response_id { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string content { get; set; } = string.Empty;

        [Required]
        public int user_id { get; set; }

        public int likes { get; set; } = 0;

        [Required]
        public int question_id { get; set; }

        public DateTime date { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        [ForeignKey("question_id")]
        public virtual Question Question { get; set; }
    }
}