using FluentValidation;
using FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport;

namespace FoodDiary.Modules.ContentReports.Application.Commands.DismissContentReport;

public sealed class DismissContentReportCommandValidator : AbstractValidator<DismissContentReportCommand> {
    public DismissContentReportCommandValidator() {
        RuleFor(command => command.AdminNote)
            .Must(note => note is null || note.Trim().Length <= 2000)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Admin note must be at most 2000 characters.");
    }
}
