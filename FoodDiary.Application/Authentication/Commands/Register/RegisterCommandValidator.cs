using FluentValidation;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FluentValidation.Results;

namespace FoodDiary.Application.Authentication.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private readonly IUserRepository _userRepository;

    public RegisterCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Email is required")
            .EmailAddress()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid email format")
            .CustomAsync(ValidateEmailAsync);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Password must be at least 6 characters");
    }

    private async Task ValidateEmailAsync(string email, ValidationContext<RegisterCommand> context, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailIncludingDeletedAsync(email);
        if (user is null)
        {
            return;
        }

        if (user.DeletedAt is not null)
        {
            context.AddFailure(new ValidationFailure(
                nameof(RegisterCommand.Email),
                "Account is scheduled for deletion.")
            {
                ErrorCode = "Authentication.AccountDeleted",
            });
            return;
        }

        context.AddFailure(new ValidationFailure(
            nameof(RegisterCommand.Email),
            "User with this email already exists.")
        {
            ErrorCode = "Validation.Conflict",
        });
    }
}
