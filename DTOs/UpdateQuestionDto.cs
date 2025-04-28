using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace jool_backend.DTOs
{
    public class UpdateQuestionDto
    {
        [Required]
        [StringLength(255)]
        public string title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string content { get; set; } = string.Empty;

        // Lista de hashtags para asociar a la pregunta (opcional)
        public List<string> hashtags { get; set; } = new List<string>();
    }
}