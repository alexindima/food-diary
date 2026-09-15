using FluentValidation;

namespace FoodDiary.Modules.Cycles.Application.Commands.ConfirmPeriodStart;

public sealed class ConfirmPeriodStartCommandValidator : AbstractValidator<ConfirmPeriodStartCommand> {
    public ConfirmPeriodStartCommandValidator() {
        RuleFor(command => command.CycleProfileId).NotEmpty();
        RuleFor(command => command.Date).NotEmpty();
    }
}
