using FluentValidation;

namespace FoodDiary.Application.Cycles.Commands.DeleteCycleProfile;

public sealed class DeleteCycleProfileCommandValidator : AbstractValidator<DeleteCycleProfileCommand> {
    public DeleteCycleProfileCommandValidator() {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CycleProfileId).NotEmpty();
    }
}
