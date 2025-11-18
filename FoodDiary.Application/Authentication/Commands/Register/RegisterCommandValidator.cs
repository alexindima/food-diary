using FluentValidation;
using FoodDiary.Application.Common.Interfaces.Persistence;

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
            .MustAsync(BeUniqueEmail)
            .WithErrorCode("Validation.Conflict")
            .WithMessage("User with this email already exists");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Password must be at least 6 characters");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user is null;
    }
}
