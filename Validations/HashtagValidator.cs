using FluentValidation;
using jool_backend.DTOs;
using jool_backend.Repository;
using System.Threading;
using System.Threading.Tasks;

namespace jool_backend.Validations
{
    public class CreateHashtagValidator : AbstractValidator<CreateHashtagDto>
    {
        private readonly HashtagRepository _repository;

        public CreateHashtagValidator(HashtagRepository repository)
        {
            _repository = repository;

            RuleFor(h => h.name)
                .NotEmpty().WithMessage("El nombre del hashtag no puede estar vacío")
                .MaximumLength(100).WithMessage("El nombre del hashtag no puede exceder los 100 caracteres")
                .MinimumLength(1).WithMessage("El nombre del hashtag debe tener al menos 1 caracter")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("El hashtag solo puede contener letras, números y guiones bajos");
        }
    }

    public class UpdateHashtagValidator : AbstractValidator<UpdateHashtagDto>
    {
        private readonly HashtagRepository _repository;

        public UpdateHashtagValidator(HashtagRepository repository)
        {
            _repository = repository;

            RuleFor(h => h.name)
                .NotEmpty().WithMessage("El nombre del hashtag no puede estar vacío")
                .MaximumLength(100).WithMessage("El nombre del hashtag no puede exceder los 100 caracteres")
                .MinimumLength(1).WithMessage("El nombre del hashtag debe tener al menos 1 caracter")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("El hashtag solo puede contener letras, números y guiones bajos");
        }
    }
}