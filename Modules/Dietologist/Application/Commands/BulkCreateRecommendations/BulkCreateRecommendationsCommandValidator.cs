using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.BulkCreateRecommendations;

public sealed class BulkCreateRecommendationsCommandValidator : AbstractValidator<BulkCreateRecommendationsCommand> {
    private const int MaxRecipients = 100;

    public BulkCreateRecommendationsCommandValidator() {
        RuleFor(command => command.ClientUserIds)
            .NotEmpty()
            .Must(ids => ids.Count <= MaxRecipients)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .Must(ids => ids.Distinct().Count() == ids.Count);
        RuleFor(command => command.Text).NotEmpty().MaximumLength(2000);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(100);
    }
}
