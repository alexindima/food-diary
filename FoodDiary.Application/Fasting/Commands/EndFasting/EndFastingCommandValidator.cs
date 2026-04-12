using FluentValidation;

namespace FoodDiary.Application.Fasting.Commands.EndFasting;

public sealed class EndFastingCommandValidator : AbstractValidator<EndFastingCommand> {
    public EndFastingCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
