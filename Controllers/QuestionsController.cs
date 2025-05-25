using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using jool_backend.DTOs;
using jool_backend.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace jool_backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class QuestionsController : ControllerBase
    {
        private readonly QuestionService _questionService;

        public QuestionsController(QuestionService questionService)
        {
            _questionService = questionService;
        }

        // GET: api/questions
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions()
        {
            var questions = await _questionService.GetAllQuestionsAsync();
            if (questions == null || !questions.Any())
            {
                return NoContent();
            }
            return Ok(questions);
        }

        // GET: api/questions/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuestionDto>> GetQuestion(int id)
        {
            var question = await _questionService.GetQuestionByIdAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            return Ok(question);
        }

        // GET: api/questions/user/5
        [HttpGet("user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestionsByUser(int userId)
        {
            var questions = await _questionService.GetQuestionsByUserIdAsync(userId);
            if (questions == null || !questions.Any())
            {
                return NoContent();
            }
            return Ok(questions);
        }

        // GET: api/questions/hashtag/programming
        [HttpGet("hashtag/{hashtagName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestionsByHashtag(string hashtagName)
        {
            var questions = await _questionService.GetQuestionsByHashtagAsync(hashtagName);
            if (questions == null || !questions.Any())
            {
                return NoContent();
            }
            return Ok(questions);
        }

        // POST: api/questions
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<QuestionDto>> CreateQuestion(CreateQuestionDto createQuestionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _questionService.CreateQuestionAsync(createQuestionDto);
            if (result == null)
            {
                return BadRequest("No se pudo crear la pregunta");
            }

            return CreatedAtAction(nameof(GetQuestion), new { id = result.question_id }, result);
        }

        // PUT: api/questions/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateQuestion(int id, UpdateQuestionDto updateQuestionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _questionService.UpdateQuestionAsync(id, updateQuestionDto);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        // DELETE: api/questions/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var result = await _questionService.DeleteQuestionAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}