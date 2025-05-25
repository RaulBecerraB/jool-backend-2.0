using System.ComponentModel.DataAnnotations;

namespace jool_backend.DTOs
{
    public class CreateResponseDto
    {
        [Required]
        [StringLength(5000)]
        public string content { get; set; } = string.Empty;

        [Required]
        public int user_id { get; set; }

        [Required]
        public int question_id { get; set; }
    }
} 