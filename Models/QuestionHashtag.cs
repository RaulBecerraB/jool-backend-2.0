using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace jool_backend.Models
{
    // Tabla de relación muchos a muchos entre Questions y Hashtags
    public class QuestionHashtag
    {
        [Required]
        public int question_id { get; set; }

        [Required]
        public int hashtag_id { get; set; }

        // Propiedades de navegación
        [ForeignKey("question_id")]
        public virtual Question Question { get; set; }

        [ForeignKey("hashtag_id")]
        public virtual Hashtag Hashtag { get; set; }
    }
}