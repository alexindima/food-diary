using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandValidator : AbstractValidator<UpdateAdminUserCommand>
{
    public UpdateAdminUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithErrorCode("Validation.Required");
    }
}
