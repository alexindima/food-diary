using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendationComment;

public sealed class CreateRecommendationCommentCommandValidator : AbstractValidator<CreateRecommendationCommentCommand> {
    public CreateRecommendationCommentCommandValidator() {
        RuleFor(command => command.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(command => command.RecommendationId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");

        RuleFor(command => command.Text)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .MaximumLength(2000)
            .WithErrorCode("Validation.Invalid");
    }
}
