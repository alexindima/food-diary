using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.CreateClientTask;

public sealed class CreateClientTaskCommandValidator : AbstractValidator<CreateClientTaskCommand> {
    public CreateClientTaskCommandValidator() {
        RuleFor(command => command.ClientUserId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Details).MaximumLength(2000);
    }
}
