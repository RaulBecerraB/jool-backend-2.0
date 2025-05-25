using System.ComponentModel.DataAnnotations;

namespace jool_backend.DTOs
{
    public class UpdateResponseDto
    {
        [Required]
        [StringLength(5000)]
        public string content { get; set; } = string.Empty;
    }
} 