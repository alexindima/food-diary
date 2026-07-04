using FluentValidation;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Consumptions.Common.Validators;

public sealed class ConsumptionAiSessionInputValidator : AbstractValidator<ConsumptionAiSessionInput> {
    private const int NotesMaxLength = 2048;

    public ConsumptionAiSessionInputValidator() {
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

        RuleForEach(x => x.Items)
            .SetValidator(new ConsumptionAiItemInputValidator());
    }
}
