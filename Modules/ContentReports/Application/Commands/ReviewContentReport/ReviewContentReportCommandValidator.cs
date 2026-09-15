using FluentValidation;
using FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport;

namespace FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport;

public sealed class ReviewContentReportCommandValidator : AbstractValidator<ReviewContentReportCommand> {
    public ReviewContentReportCommandValidator() {
        RuleFor(command => command.AdminNote)
            .Must(note => note is null || note.Trim().Length <= 2000)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Admin note must be at most 2000 characters.");
    }
}
