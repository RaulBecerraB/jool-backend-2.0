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

        public async Task<ResponseDto?> CreateResponseAsync(CreateResponseDto createDto)
        {
            // Verificar que la pregunta existe
            var question = await _context.Questions.FindAsync(createDto.question_id);
            if (question == null)
            {
                return null;
            }

            // Verificar que el usuario existe
            var user = await _context.Users.FindAsync(createDto.user_id);
            if (user == null)
            {
                return null;
            }

            var response = new Response
            {
                content = createDto.content,
                user_id = createDto.user_id,
                question_id = createDto.question_id,
                likes = 0,
                date = DateTime.Now
            };

            _context.Responses.Add(response);
            await _context.SaveChangesAsync();

            // Cargar el usuario para el mapeo
            await _context.Entry(response).Reference(r => r.User).LoadAsync();

            return MapToDto(response);
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
            {
                return false;
            }

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

        private static ResponseDto MapToDto(Response response)
        {
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
    }
} 