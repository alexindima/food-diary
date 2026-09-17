using FluentValidation;

namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvices;

public sealed class ImportAdminDailyAdvicesCommandValidator : AbstractValidator<ImportAdminDailyAdvicesCommand> {
    public ImportAdminDailyAdvicesCommandValidator() {
        RuleFor(command => command.Version).Equal(1).WithErrorCode("Validation.Invalid")
            .WithMessage("Unsupported advice import format version.");
        RuleFor(command => command.Advices).Cascade(CascadeMode.Stop).NotEmpty().WithErrorCode("Validation.Required")
            .Must(items => items.Count <= 1000).WithErrorCode("Validation.Invalid");
        RuleForEach(command => command.Advices).NotNull().WithErrorCode("Validation.Required");
    }
}
