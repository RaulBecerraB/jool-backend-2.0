using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace jool_backend.Models
{
    public class Question
    {
        [Key]
        public int question_id { get; set; }

        [Required]
        [Column(TypeName = "varchar(255)")]
        public string title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "text")]
        public string content { get; set; } = string.Empty;

        [Required]
        public int user_id { get; set; }

        public int views { get; set; } = 0;

        public int stars { get; set; } = 0;

        public DateTime date { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        public virtual ICollection<Response> Responses { get; set; } = new List<Response>();

        public virtual ICollection<QuestionHashtag> QuestionHashtags { get; set; } = new List<QuestionHashtag>();
    }
}