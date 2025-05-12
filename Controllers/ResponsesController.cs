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
    public class ResponsesController : ControllerBase
    {
        private readonly ResponseService _responseService;

        public ResponsesController(ResponseService responseService)
        {
            _responseService = responseService;
        }

        // GET: api/responses
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<IEnumerable<ResponseDto>>> GetResponses()
        {
            var responses = await _responseService.GetAllResponsesAsync();
            if (responses == null || !responses.Any())
            {
                return NoContent();
            }
            return Ok(responses);
        }

        // GET: api/responses/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResponseDto>> GetResponse(int id)
        {
            var response = await _responseService.GetResponseByIdAsync(id);

            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        // GET: api/responses/question/5
        [HttpGet("question/{questionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<IEnumerable<ResponseDto>>> GetResponsesByQuestion(int questionId)
        {
            var responses = await _responseService.GetResponsesByQuestionIdAsync(questionId);
            if (responses == null || !responses.Any())
            {
                return NoContent();
            }
            return Ok(responses);
        }

        // POST: api/responses
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ResponseDto>> CreateResponse(CreateResponseDto createResponseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _responseService.CreateResponseAsync(createResponseDto);
            if (result == null)
            {
                return BadRequest("No se pudo crear la respuesta. Verifique que la pregunta y el usuario existan.");
            }

            return CreatedAtAction(nameof(GetResponse), new { id = result.response_id }, result);
        }

        // PUT: api/responses/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResponse(int id, UpdateResponseDto updateResponseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _responseService.UpdateResponseAsync(id, updateResponseDto);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        // DELETE: api/responses/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteResponse(int id)
        {
            var result = await _responseService.DeleteResponseAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/responses/5/like
        [HttpPost("{id}/like")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LikeResponse(int id)
        {
            var result = await _responseService.LikeResponseAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Like agregado exitosamente" });
        }
    }
} 