using FluentValidation;
using OrderFlow.Identity.Controllers;

// Validador para LoginRequest
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Regla para Email
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")                  // No puede ir vacío
            .EmailAddress().WithMessage("Email format is invalid")        // Debe tener formato de email
            .MaximumLength(256).WithMessage("Email is too long");         // Evitar emails más largos que la columna de Identity

        // Regla para Password
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")               // No puede ir vacía
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
            // Longitud mínima
            .MaximumLength(100).WithMessage("Password is too long");      // Limitar longitud para evitar passwords absurdamente largas
                                                                          // Si quieres complejidad, puedes añadir un Matches() más abajo
    }
}

// Validador para RegisterRequest
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        // Regla para Email
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")                  // Obligatorio
            .EmailAddress().WithMessage("Email format is invalid")        // Formato de email
            .MaximumLength(256).WithMessage("Email is too long");         // Alineado con Identity

        // Regla para Password
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")               // Obligatoria
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
            // Longitud mínima
            .MaximumLength(100).WithMessage("Password is too long");      // Longitud máxima
                                                                          // Aquí puedes meter reglas de complejidad: mayúsculas, minúsculas, números, etc.

        // ConfirmPassword
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("ConfirmPassword is required")        // Obligatoria
            .Equal(x => x.Password).WithMessage("Passwords do not match");// Debe ser igual a Password
    }
}


