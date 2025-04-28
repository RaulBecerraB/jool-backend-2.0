using jool_backend.Models;
using jool_backend.Repository;
using jool_backend.DTOs;
using jool_backend.Validations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace jool_backend.Services
{
    public class QuestionService
    {
        private readonly QuestionRepository _questionRepository;
        private readonly HashtagRepository _hashtagRepository;
        private readonly CreateQuestionValidatorAsync _asyncValidator;

        public QuestionService(
            QuestionRepository questionRepository,
            HashtagRepository hashtagRepository,
            CreateQuestionValidatorAsync asyncValidator)
        {
            _questionRepository = questionRepository;
            _hashtagRepository = hashtagRepository;
            _asyncValidator = asyncValidator;
        }

        public async Task<IEnumerable<QuestionDto>> GetAllQuestionsAsync()
        {
            var questions = await _questionRepository.GetAllQuestionsAsync();
            return questions.Select(q => MapToDto(q));
        }

        public async Task<QuestionDto?> GetQuestionByIdAsync(int id)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(id);
            return question != null ? MapToDto(question) : null;
        }

        public async Task<IEnumerable<QuestionDto>> GetQuestionsByUserIdAsync(int userId)
        {
            var questions = await _questionRepository.GetQuestionsByUserIdAsync(userId);
            return questions.Select(q => MapToDto(q));
        }

        public async Task<IEnumerable<QuestionDto>> GetQuestionsByHashtagAsync(string hashtagName)
        {
            var questions = await _questionRepository.GetQuestionsByHashtagAsync(hashtagName);
            return questions.Select(q => MapToDto(q));
        }

        public async Task<QuestionDto?> CreateQuestionAsync(CreateQuestionDto createDto)
        {
            // Validate using async validator
            ValidationResult validationResult = await _asyncValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                return null;
            }

            // Crear pregunta
            var question = new Question
            {
                title = createDto.title,
                content = createDto.content,
                user_id = createDto.user_id,
                views = 0,
                stars = 0,
                date = DateTime.Now
            };

            var createdQuestion = await _questionRepository.CreateQuestionAsync(question);

            // Procesar hashtags
            if (createDto.hashtags != null && createDto.hashtags.Count > 0)
            {
                await ProcessHashtagsAsync(createdQuestion, createDto.hashtags);
            }

            // Recargar la pregunta con sus relaciones
            var savedQuestion = await _questionRepository.GetQuestionByIdAsync(createdQuestion.question_id);
            return savedQuestion != null ? MapToDto(savedQuestion) : null;
        }

        public async Task<QuestionDto?> UpdateQuestionAsync(int id, UpdateQuestionDto updateDto)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return null;
            }

            // Actualizar propiedades básicas
            question.title = updateDto.title;
            question.content = updateDto.content;

            // Eliminar relaciones existentes con hashtags
            foreach (var qh in question.QuestionHashtags.ToList())
            {
                question.QuestionHashtags.Remove(qh);
            }

            // Guardar cambios básicos
            var success = await _questionRepository.UpdateQuestionAsync(question);
            if (!success)
            {
                return null;
            }

            // Procesar nuevos hashtags
            if (updateDto.hashtags != null && updateDto.hashtags.Count > 0)
            {
                await ProcessHashtagsAsync(question, updateDto.hashtags);
            }

            // Recargar la pregunta con sus relaciones
            var updatedQuestion = await _questionRepository.GetQuestionByIdAsync(id);
            return updatedQuestion != null ? MapToDto(updatedQuestion) : null;
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            return await _questionRepository.DeleteQuestionAsync(id);
        }

        private async Task ProcessHashtagsAsync(Question question, List<string> hashtagNames)
        {
            foreach (var name in hashtagNames.Distinct())
            {
                // Verificar si el hashtag ya existe
                var hashtag = await _hashtagRepository.GetHashtagByNameAsync(name);

                if (hashtag == null)
                {
                    // Crear nuevo hashtag
                    hashtag = new Hashtag
                    {
                        name = name,
                        used_count = 1
                    };
                    await _hashtagRepository.CreateHashtagAsync(hashtag);
                }
                else
                {
                    // Incrementar contador de uso
                    hashtag.used_count++;
                    await _hashtagRepository.UpdateHashtagAsync(hashtag);
                }

                // Asociar hashtag a la pregunta
                var questionHashtag = new QuestionHashtag
                {
                    question_id = question.question_id,
                    hashtag_id = hashtag.hashtag_id
                };

                question.QuestionHashtags.Add(questionHashtag);
            }

            await _questionRepository.UpdateQuestionAsync(question);
        }

        private static QuestionDto MapToDto(Question question)
        {
            return new QuestionDto
            {
                question_id = question.question_id,
                title = question.title,
                content = question.content,
                user_id = question.user_id,
                views = question.views,
                date = question.date,
                user_name = $"{question.User.first_name} {question.User.last_name}",
                hashtags = question.QuestionHashtags
                    .Select(qh => new HashtagDto
                    {
                        hashtag_id = qh.Hashtag.hashtag_id,
                        name = qh.Hashtag.name,
                        used_count = qh.Hashtag.used_count
                    })
                    .ToList()
            };
        }
    }
}