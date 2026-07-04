using FluentValidation;
using FoodDiary.Application.Authentication.Common;
using FluentValidation.Results;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand> {
    private readonly IAuthenticationUserRegistrationService _userRegistrationService;

    public RegisterCommandValidator(IAuthenticationUserRegistrationService userRegistrationService) {
        _userRegistrationService = userRegistrationService;

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

    private async Task ValidateEmailAsync(string email, ValidationContext<RegisterCommand> context, CancellationToken cancellationToken) {
        User? user = await _userRegistrationService.GetByEmailIncludingDeletedAsync(email, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return;
        }

        if (user.DeletedAt is not null) {
            context.AddFailure(new ValidationFailure(
                nameof(RegisterCommand.Email),
                "Account is scheduled for deletion.") {
                ErrorCode = "Authentication.AccountDeleted",
            });
            return;
        }

        context.AddFailure(new ValidationFailure(
            nameof(RegisterCommand.Email),
            "User with this email already exists.") {
            ErrorCode = "Validation.Conflict",
        });
    }
}
