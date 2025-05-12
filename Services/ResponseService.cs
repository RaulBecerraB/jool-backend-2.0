using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using jool_backend.Repository;
using jool_backend.DTOs;
using jool_backend.Models;

namespace jool_backend.Services
{
    public class ResponseService
    {
        private readonly JoolContext _context;

        public ResponseService(JoolContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ResponseDto>> GetAllResponsesAsync()
        {
            return await _context.Responses
                .Include(r => r.User)
                .Select(r => new ResponseDto
                {
                    response_id = r.response_id,
                    content = r.content,
                    user_id = r.user_id,
                    likes = r.likes,
                    question_id = r.question_id,
                    date = r.date,
                    user_name = $"{r.User.first_name} {r.User.last_name}"
                })
                .ToListAsync();
        }

        public async Task<ResponseDto?> GetResponseByIdAsync(int id)
        {
            var response = await _context.Responses
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.response_id == id);

            if (response == null)
                return null;

            return new ResponseDto
            {
                response_id = response.response_id,
                content = response.content,
                user_id = response.user_id,
                likes = response.likes,
                question_id = response.question_id,
                date = response.date,
                user_name = $"{response.User.first_name} {response.User.last_name}"
            };
        }

        public async Task<IEnumerable<ResponseDto>> GetResponsesByQuestionIdAsync(int questionId)
        {
            return await _context.Responses
                .Include(r => r.User)
                .Where(r => r.question_id == questionId)
                .Select(r => new ResponseDto
                {
                    response_id = r.response_id,
                    content = r.content,
                    user_id = r.user_id,
                    likes = r.likes,
                    question_id = r.question_id,
                    date = r.date,
                    user_name = $"{r.User.first_name} {r.User.last_name}"
                })
                .ToListAsync();
        }

        public async Task<ResponseDto?> CreateResponseAsync(CreateResponseDto createResponseDto)
        {
            // Verificar que la pregunta existe
            var questionExists = await _context.Questions.AnyAsync(q => q.question_id == createResponseDto.question_id);
            if (!questionExists)
                return null;

            // Verificar que el usuario existe
            var userExists = await _context.Users.AnyAsync(u => u.user_id == createResponseDto.user_id);
            if (!userExists)
                return null;

            var response = new Response
            {
                content = createResponseDto.content,
                user_id = createResponseDto.user_id,
                question_id = createResponseDto.question_id,
                date = DateTime.Now,
                likes = 0
            };

            _context.Responses.Add(response);
            await _context.SaveChangesAsync();

            var createdResponse = await _context.Responses
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.response_id == response.response_id);

            if (createdResponse == null)
                return null;

            return new ResponseDto
            {
                response_id = createdResponse.response_id,
                content = createdResponse.content,
                user_id = createdResponse.user_id,
                likes = createdResponse.likes,
                question_id = createdResponse.question_id,
                date = createdResponse.date,
                user_name = $"{createdResponse.User.first_name} {createdResponse.User.last_name}"
            };
        }

        public async Task<ResponseDto?> UpdateResponseAsync(int id, UpdateResponseDto updateResponseDto)
        {
            var response = await _context.Responses
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.response_id == id);

            if (response == null)
                return null;

            response.content = updateResponseDto.content;
            await _context.SaveChangesAsync();

            return new ResponseDto
            {
                response_id = response.response_id,
                content = response.content,
                user_id = response.user_id,
                likes = response.likes,
                question_id = response.question_id,
                date = response.date,
                user_name = $"{response.User.first_name} {response.User.last_name}"
            };
        }

        public async Task<bool> DeleteResponseAsync(int id)
        {
            var response = await _context.Responses.FindAsync(id);
            if (response == null)
                return false;

            _context.Responses.Remove(response);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LikeResponseAsync(int id)
        {
            var response = await _context.Responses.FindAsync(id);
            if (response == null)
                return false;

            response.likes++;
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 