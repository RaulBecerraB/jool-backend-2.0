using FluentValidation;
using jool_backend.DTOs;
using jool_backend.Repository;
using System.Threading;
using System.Threading.Tasks;

namespace jool_backend.Validations
{
    public class CreateQuestionValidator : AbstractValidator<CreateQuestionDto>
    {
        private readonly JoolContext _context;

        public CreateQuestionValidator(JoolContext context)
        {
            _context = context;

            RuleFor(q => q.title)
                .NotEmpty().WithMessage("El título de la pregunta no puede estar vacío")
                .MaximumLength(255).WithMessage("El título no puede exceder los 255 caracteres");

            RuleFor(q => q.content)
                .NotEmpty().WithMessage("El contenido de la pregunta no puede estar vacío")
                .MaximumLength(5000).WithMessage("El contenido no puede exceder los 5000 caracteres");

            RuleFor(q => q.user_id)
                .NotEmpty().WithMessage("El ID de usuario es requerido")
                .MustAsync(UserExistsAsync).WithMessage("El usuario especificado no existe");

            RuleForEach(q => q.hashtags)
                .NotEmpty().WithMessage("Un hashtag no puede estar vacío")
                .MaximumLength(100).WithMessage("Un hashtag no puede exceder los 100 caracteres")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Los hashtags solo pueden contener letras, números y guiones bajos");
        }

        private async Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken)
        {
            return await _context.Users.FindAsync(new object[] { userId }, cancellationToken) != null;
        }
    }

    public class UpdateQuestionValidator : AbstractValidator<UpdateQuestionDto>
    {
        public UpdateQuestionValidator()
        {
            RuleFor(q => q.title)
                .NotEmpty().WithMessage("El título de la pregunta no puede estar vacío")
                .MaximumLength(255).WithMessage("El título no puede exceder los 255 caracteres");

            RuleFor(q => q.content)
                .NotEmpty().WithMessage("El contenido de la pregunta no puede estar vacío")
                .MaximumLength(5000).WithMessage("El contenido no puede exceder los 5000 caracteres");

            RuleForEach(q => q.hashtags)
                .NotEmpty().WithMessage("Un hashtag no puede estar vacío")
                .MaximumLength(100).WithMessage("Un hashtag no puede exceder los 100 caracteres")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Los hashtags solo pueden contener letras, números y guiones bajos");
        }
    }
}