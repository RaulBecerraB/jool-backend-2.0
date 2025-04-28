using jool_backend.Models;
using jool_backend.Repository;
using jool_backend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace jool_backend.Services
{
    public class HashtagService
    {
        private readonly HashtagRepository _repository;

        public HashtagService(HashtagRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<HashtagDto>> GetAllHashtagsAsync()
        {
            var hashtags = await _repository.GetAllHashtagsAsync();
            return hashtags.Select(h => MapToDto(h));
        }

        public async Task<HashtagDto?> GetHashtagByIdAsync(int id)
        {
            var hashtag = await _repository.GetHashtagByIdAsync(id);
            return hashtag != null ? MapToDto(hashtag) : null;
        }

        public async Task<HashtagDto?> CreateHashtagAsync(CreateHashtagDto createDto)
        {
            // Crear nuevo hashtag
            var hashtag = new Hashtag
            {
                name = createDto.name,
                used_count = 1
            };

            var createdHashtag = await _repository.CreateHashtagAsync(hashtag);
            return MapToDto(createdHashtag);
        }

        public async Task<HashtagDto?> UpdateHashtagAsync(int id, UpdateHashtagDto updateDto)
        {
            var hashtag = await _repository.GetHashtagByIdAsync(id);
            if (hashtag == null)
            {
                return null;
            }

            hashtag.name = updateDto.name;

            var success = await _repository.UpdateHashtagAsync(hashtag);
            return success ? MapToDto(hashtag) : null;
        }

        public async Task<bool> DeleteHashtagAsync(int id)
        {
            return await _repository.DeleteHashtagAsync(id);
        }

        private static HashtagDto MapToDto(Hashtag hashtag)
        {
            return new HashtagDto
            {
                hashtag_id = hashtag.hashtag_id,
                name = hashtag.name,
                used_count = hashtag.used_count
            };
        }
    }
}