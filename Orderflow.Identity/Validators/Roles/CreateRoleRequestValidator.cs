using FluentValidation;
using Orderflow.Identity.DTOs.Roles.Requests;
namespace OrderFlow.Identity.Validators.Roles;

/// <summary>
/// Validator for CreateRoleRequest
/// </summary>
public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(256).WithMessage("Role name must not exceed 256 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Role name can only contain letters, numbers, underscores, and hyphens");
    }
}
