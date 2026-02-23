using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandValidator : AbstractValidator<UpdateAdminUserCommand> {
    public UpdateAdminUserCommandValidator() {
        RuleFor(x => x.UserId).NotEmpty().WithErrorCode("Validation.Required");

        RuleFor(x => x.AiInputTokenLimit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AiInputTokenLimit.HasValue)
            .WithMessage("AI input token limit must be non-negative.");

        RuleFor(x => x.AiOutputTokenLimit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AiOutputTokenLimit.HasValue)
            .WithMessage("AI output token limit must be non-negative.");

        RuleFor(x => x.Language)
            .Must(value => value is null || LanguageCode.TryParse(value, out _))
            .WithMessage("Invalid language value.");

        When(x => x.Roles is not null, () => {
            RuleForEach(x => x.Roles!)
                .Must(role => !string.IsNullOrWhiteSpace(role) &&
                              (string.Equals(role.Trim(), RoleNames.Admin, StringComparison.Ordinal) ||
                               string.Equals(role.Trim(), RoleNames.Premium, StringComparison.Ordinal) ||
                               string.Equals(role.Trim(), RoleNames.Support, StringComparison.Ordinal)))
                .WithMessage("Unknown role.");
        });
    }
}
