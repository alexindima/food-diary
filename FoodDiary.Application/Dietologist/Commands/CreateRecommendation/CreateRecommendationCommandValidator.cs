using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendation;

public class CreateRecommendationCommandValidator : AbstractValidator<CreateRecommendationCommand> {
    public CreateRecommendationCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.ClientUserId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Client user ID is required");

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Recommendation text is required")
            .MaximumLength(2000)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Recommendation text must be at most 2000 characters");
    }
}
