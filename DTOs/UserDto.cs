namespace jool_backend.DTOs
{
    public class UserDto
    {
        public int user_id { get; set; }
        public string first_name { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public bool is_active { get; set; }
        public string? phone { get; set; }

        // Para la imagen, se devuelve un indicador de si existe o no
        public bool has_image { get; set; }

        // Token de autenticación
        public TokenDto? Token { get; set; }
    }
}