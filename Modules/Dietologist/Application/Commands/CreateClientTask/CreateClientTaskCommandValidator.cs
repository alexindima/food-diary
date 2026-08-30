using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.CreateClientTask;

public sealed class CreateClientTaskCommandValidator : AbstractValidator<CreateClientTaskCommand> {
    public CreateClientTaskCommandValidator() {
        RuleFor(command => command.ClientUserId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Details).MaximumLength(2000);
        RuleFor(command => command.DueAtUtc)
            .Must(static value => !value.HasValue || value.Value.Kind != DateTimeKind.Unspecified)
            .WithMessage("DueAtUtc must include a UTC offset.");
    }
}
