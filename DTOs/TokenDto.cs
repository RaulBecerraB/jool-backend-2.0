using System;

namespace jool_backend.DTOs
{
    public class TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
} 