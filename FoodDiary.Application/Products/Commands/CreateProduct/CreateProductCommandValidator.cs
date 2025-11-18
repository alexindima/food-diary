using FluentValidation;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand> {
    private readonly IUserRepository _userRepository;

    public CreateProductCommandValidator(IUserRepository userRepository) {
        _userRepository = userRepository;

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .MustAsync(UserExists)
            .WithErrorCode("Validation.NotFound")
            .WithMessage("User not found");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Name is required");

        RuleFor(x => x.BaseUnit)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("BaseUnit is required")
            .Must(BeValidUnit)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid measurement unit");

        RuleFor(x => x.Visibility)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Visibility is required")
            .Must(BeValidVisibility)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid visibility level");

        RuleFor(x => x.BaseAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("BaseAmount must be greater than 0");

        RuleFor(x => x.CaloriesPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CaloriesPerBase must be non-negative");

        RuleFor(x => x.ProteinsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ProteinsPerBase must be non-negative");

        RuleFor(x => x.FatsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FatsPerBase must be non-negative");

        RuleFor(x => x.CarbsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CarbsPerBase must be non-negative");

        RuleFor(x => x.FiberPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FiberPerBase must be non-negative");
    }

    private static bool BeValidUnit(string unit) {
        return Enum.TryParse(unit, ignoreCase: true, out MeasurementUnit _);
    }

    private static bool BeValidVisibility(string visibility) {
        return Enum.TryParse(visibility, ignoreCase: true, out Visibility _);
    }

    private async Task<bool> UserExists(UserId? userId, CancellationToken cancellationToken) {
        if (userId is null) {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);
        return user is not null;
    }
}
