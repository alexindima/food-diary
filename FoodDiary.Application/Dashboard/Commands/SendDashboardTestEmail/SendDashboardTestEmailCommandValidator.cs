using FluentValidation;

namespace FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;

public sealed class SendDashboardTestEmailCommandValidator : AbstractValidator<SendDashboardTestEmailCommand> {
    public SendDashboardTestEmailCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
