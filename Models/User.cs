using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace jool_backend.Models
{
    public class User
    {
        [Key]
        public int user_id { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string first_name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string last_name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(255)")]
        public string email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(255)")]
        public string password { get; set; } = string.Empty;

        public bool is_active { get; set; } = true;

        public byte[]? image { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? phone { get; set; }

        // Propiedades de navegación
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<Response> Responses { get; set; } = new List<Response>();
    }
}