using System;

namespace jool_backend.DTOs
{
    public class ResponseDto
    {
        public int response_id { get; set; }
        public string content { get; set; } = string.Empty;
        public int user_id { get; set; }
        public int likes { get; set; }
        public int question_id { get; set; }
        public DateTime date { get; set; }

        // Información básica del usuario
        public string user_name { get; set; } = string.Empty;
    }
} 