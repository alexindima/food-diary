using FluentValidation;
using FoodDiary.Application.Meals.Common.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Meals.Common.Validators;

public sealed class MealAiSessionInputValidator : AbstractValidator<MealAiSessionInput> {
    private const int NotesMaxLength = 2048;

    public MealAiSessionInputValidator() {
        RuleFor(x => x.Source)
            .Must(EnumValueParser.CanParseOptional<AiRecognitionSource>)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Unknown AI recognition source value.");

        RuleFor(x => x.RecognizedAtUtc)
            .Must(value => value is not { Kind: DateTimeKind.Unspecified })
            .WithErrorCode("Validation.Invalid")
            .WithMessage("RecognizedAtUtc timestamp kind must be specified.");

        RuleFor(x => x.Notes)
            .MaximumLength(NotesMaxLength)
            .When(x => x.Notes is not null)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Notes must be at most {NotesMaxLength} characters.");

        RuleFor(x => x.Items)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("AI session items are required.");

        RuleForEach(x => x.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AI session items must not contain null elements.")
            .SetValidator(new MealAiItemInputValidator());
    }
}
