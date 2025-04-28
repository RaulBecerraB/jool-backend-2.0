using System.ComponentModel.DataAnnotations;

namespace jool_backend.DTOs
{
    public class CreateHashtagDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string name { get; set; } = string.Empty;
    }
}