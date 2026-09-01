using FluentValidation;

namespace FoodDiary.Application.Identity.Authentication.Commands.LinkGoogle;

public sealed class LinkGoogleCommandValidator : AbstractValidator<LinkGoogleCommand> {
    public LinkGoogleCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");

        RuleFor(x => x.Credential)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("credential is required.");
    }
}
