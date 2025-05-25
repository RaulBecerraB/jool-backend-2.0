using Microsoft.EntityFrameworkCore;
using jool_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace jool_backend.Repository
{
    public class QuestionRepository
    {
        private readonly JoolContext _context;

        public QuestionRepository(JoolContext context)
        {
            _context = context;
        }

        public async Task<List<Question>> GetAllQuestionsAsync()
        {
            var questions = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionHashtags)
                    .ThenInclude(qh => qh.Hashtag)
                .OrderByDescending(q => q.date)
                .ToListAsync();

            // Obtener conteos de respuestas para todas las preguntas
            var questionIds = questions.Select(q => q.question_id).ToList();
            var responseCounts = await _context.Responses
                .Where(r => questionIds.Contains(r.question_id))
                .GroupBy(r => r.question_id)
                .Select(g => new { QuestionId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Asignar conteos a las preguntas
            foreach (var question in questions)
            {
                var count = responseCounts.FirstOrDefault(rc => rc.QuestionId == question.question_id);
                question.response_count = count?.Count ?? 0;
            }

            return questions;
        }

        public async Task<Question?> GetQuestionByIdAsync(int id)
        {
            var question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionHashtags)
                    .ThenInclude(qh => qh.Hashtag)
                .Include(q => q.Responses)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(q => q.question_id == id);

            if (question != null)
            {
                // Incrementar contador de vistas
                question.views += 1;
                await _context.SaveChangesAsync();
            }

            return question;
        }

        public async Task<Question> CreateQuestionAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task<bool> UpdateQuestionAsync(Question question)
        {
            _context.Entry(question).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await QuestionExists(question.question_id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            var question = await _context.Questions
                .Include(q => q.QuestionHashtags)
                .FirstOrDefaultAsync(q => q.question_id == id);

            if (question == null)
            {
                return false;
            }

            // Eliminar relaciones con hashtags
            foreach (var qh in question.QuestionHashtags.ToList())
            {
                _context.QuestionHashtags.Remove(qh);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> QuestionExists(int id)
        {
            return await _context.Questions.AnyAsync(q => q.question_id == id);
        }

        public async Task<List<Question>> GetQuestionsByUserIdAsync(int userId)
        {
            var questions = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionHashtags)
                    .ThenInclude(qh => qh.Hashtag)
                .Where(q => q.user_id == userId)
                .OrderByDescending(q => q.date)
                .ToListAsync();

            // Obtener conteos de respuestas para todas las preguntas
            var questionIds = questions.Select(q => q.question_id).ToList();
            var responseCounts = await _context.Responses
                .Where(r => questionIds.Contains(r.question_id))
                .GroupBy(r => r.question_id)
                .Select(g => new { QuestionId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Asignar conteos a las preguntas
            foreach (var question in questions)
            {
                var count = responseCounts.FirstOrDefault(rc => rc.QuestionId == question.question_id);
                question.response_count = count?.Count ?? 0;
            }

            return questions;
        }

        public async Task<List<Question>> GetQuestionsByHashtagAsync(string hashtagName)
        {
            var questions = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionHashtags)
                    .ThenInclude(qh => qh.Hashtag)
                .Where(q => q.QuestionHashtags.Any(qh => qh.Hashtag.name == hashtagName))
                .OrderByDescending(q => q.date)
                .ToListAsync();

            // Obtener conteos de respuestas para todas las preguntas
            var questionIds = questions.Select(q => q.question_id).ToList();
            var responseCounts = await _context.Responses
                .Where(r => questionIds.Contains(r.question_id))
                .GroupBy(r => r.question_id)
                .Select(g => new { QuestionId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Asignar conteos a las preguntas
            foreach (var question in questions)
            {
                var count = responseCounts.FirstOrDefault(rc => rc.QuestionId == question.question_id);
                question.response_count = count?.Count ?? 0;
            }

            return questions;
        }
    }
}