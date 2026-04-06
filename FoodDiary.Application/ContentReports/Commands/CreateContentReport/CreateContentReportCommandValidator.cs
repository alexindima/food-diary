using FluentValidation;

namespace FoodDiary.Application.ContentReports.Commands.CreateContentReport;

public sealed class CreateContentReportCommandValidator : AbstractValidator<CreateContentReportCommand> {
    public CreateContentReportCommandValidator() {
        RuleFor(x => x.TargetType)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Target type is required.")
            .Must(value => value is "Recipe" or "Comment")
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Target type must be 'Recipe' or 'Comment'.");

        RuleFor(x => x.TargetId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("Target ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Reason is required.")
            .MaximumLength(1000)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Reason must be at most 1000 characters.");
    }
}
