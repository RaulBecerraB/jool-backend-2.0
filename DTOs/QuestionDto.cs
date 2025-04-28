using System;
using System.Collections.Generic;

namespace jool_backend.DTOs
{
    public class QuestionDto
    {
        public int question_id { get; set; }
        public string title { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
        public int user_id { get; set; }
        public int views { get; set; }
        public DateTime date { get; set; }

        // Información básica del usuario (para no devolver toda la entidad)
        public string user_name { get; set; } = string.Empty;

        // Hashtags relacionados
        public List<HashtagDto> hashtags { get; set; } = new List<HashtagDto>();
    }
}