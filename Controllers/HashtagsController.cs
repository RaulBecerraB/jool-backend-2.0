using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using jool_backend.DTOs;
using jool_backend.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace jool_backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HashtagsController : ControllerBase
    {
        private readonly HashtagService _hashtagService;

        public HashtagsController(HashtagService hashtagService)
        {
            _hashtagService = hashtagService;
        }

        // GET: api/hashtags
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<IEnumerable<HashtagDto>>> GetHashtags()
        {
            var hashtags = await _hashtagService.GetAllHashtagsAsync();
            if (hashtags == null || !hashtags.Any())
            {
                return NoContent();
            }
            return Ok(hashtags);
        }

        // GET: api/hashtags/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashtagDto>> GetHashtag(int id)
        {
            var hashtag = await _hashtagService.GetHashtagByIdAsync(id);

            if (hashtag == null)
            {
                return NotFound();
            }

            return Ok(hashtag);
        }

        // POST: api/hashtags
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<HashtagDto>> CreateHashtag(CreateHashtagDto createHashtagDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _hashtagService.CreateHashtagAsync(createHashtagDto);

            return CreatedAtAction(nameof(GetHashtag), new { id = result.hashtag_id }, result);
        }

        // PUT: api/hashtags/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateHashtag(int id, UpdateHashtagDto updateHashtagDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _hashtagService.UpdateHashtagAsync(id, updateHashtagDto);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        // DELETE: api/hashtags/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteHashtag(int id)
        {
            var result = await _hashtagService.DeleteHashtagAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
