using FluentValidation;
using Orderflow.Identity.DTOs.Users.Requests;

namespace Orderflow.Identity.Validators.Users;

/// <summary>
/// Validator for ChangePasswordRequest
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter")
            .Matches(@"\d").WithMessage("New password must contain at least one digit")
            .Matches(@"[^\da-zA-Z]").WithMessage("New password must contain at least one special character")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}
