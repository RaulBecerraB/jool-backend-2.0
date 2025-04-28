using System.ComponentModel.DataAnnotations;

namespace jool_backend.DTOs
{
    public class RegisterUserDto
    {
        [Required]
        [StringLength(100)]
        public string first_name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string last_name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(30)]
        public string email { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string password { get; set; } = string.Empty;

        [StringLength(20)]
        public string? phone { get; set; }
    }
}