using FluentValidation;
using jool_backend.DTOs;
using jool_backend.Repository;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace jool_backend.Validations
{
    // Synchronous validator for ASP.NET automatic validation
    public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
    {
        private readonly UserRepository _repository;

        public RegisterUserValidator(UserRepository repository)
        {
            _repository = repository;

            RuleFor(u => u.first_name)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres");

            RuleFor(u => u.last_name)
                .NotEmpty().WithMessage("El apellido es requerido")
                .MaximumLength(100).WithMessage("El apellido no puede exceder los 100 caracteres");

            RuleFor(u => u.email)
                .NotEmpty().WithMessage("El correo electrónico es requerido")
                .EmailAddress().WithMessage("Formato de correo electrónico inválido")
                .MaximumLength(30).WithMessage("El correo no puede exceder los 30 caracteres");

            // Removed async email validation for ASP.NET automatic validation
            RuleFor(u => u.password)
                .NotEmpty().WithMessage("La contraseña es requerida")
                .MaximumLength(30).WithMessage("La contraseña no puede exceder los 30 caracteres");

            RuleFor(u => u.phone)
                .MaximumLength(20).WithMessage("El teléfono no puede exceder los 20 caracteres")
                .Matches(@"^[0-9\+\-\s]*$").WithMessage("El teléfono solo puede contener números, +, - y espacios")
                .When(u => !string.IsNullOrEmpty(u.phone));
        }
    }

    // Keep a separate async validator for manual validation scenarios
    public class RegisterUserValidatorAsync : AbstractValidator<RegisterUserDto>
    {
        private readonly UserRepository _repository;

        public RegisterUserValidatorAsync(UserRepository repository)
        {
            _repository = repository;

            RuleFor(u => u.first_name)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres");

            RuleFor(u => u.last_name)
                .NotEmpty().WithMessage("El apellido es requerido")
                .MaximumLength(100).WithMessage("El apellido no puede exceder los 100 caracteres");

            RuleFor(u => u.email)
                .NotEmpty().WithMessage("El correo electrónico es requerido")
                .EmailAddress().WithMessage("Formato de correo electrónico inválido")
                .MaximumLength(30).WithMessage("El correo no puede exceder los 30 caracteres")
                .MustAsync(BeUniqueEmail).WithMessage("Este correo electrónico ya está registrado");

            RuleFor(u => u.password)
                .NotEmpty().WithMessage("La contraseña es requerida")
                .MaximumLength(30).WithMessage("La contraseña no puede exceder los 30 caracteres");
            RuleFor(u => u.phone)
                .MaximumLength(20).WithMessage("El teléfono no puede exceder los 20 caracteres")
                .When(u => !string.IsNullOrEmpty(u.phone));
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _repository.EmailExistsAsync(email);
        }
    }

    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(u => u.email)
                .NotEmpty().WithMessage("El correo electrónico es requerido")
                .EmailAddress().WithMessage("Formato de correo electrónico inválido");

            RuleFor(u => u.password)
                .NotEmpty().WithMessage("La contraseña es requerida");
        }
    }
}