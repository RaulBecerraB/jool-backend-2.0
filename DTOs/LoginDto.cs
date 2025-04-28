using System.ComponentModel.DataAnnotations;

namespace jool_backend.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        [Required]
        public string password { get; set; } = string.Empty;
    }
}