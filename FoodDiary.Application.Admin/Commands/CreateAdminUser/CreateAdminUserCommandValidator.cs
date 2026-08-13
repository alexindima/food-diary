using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand> {
    private static readonly HashSet<string> AllowedRoles = new(
        [RoleNames.Admin, RoleNames.Premium, RoleNames.Support, RoleNames.Dietologist],
        StringComparer.Ordinal);

    public CreateAdminUserCommandValidator() {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();
        RuleFor(command => command.ActorUserId)
            .NotEmpty();
        RuleFor(command => command.Language)
            .Must(value => value is null || LanguageCode.TryParse(value, out _))
            .WithMessage("Invalid language value.");
        RuleFor(command => command.Roles)
            .NotNull();
        RuleForEach(command => command.Roles)
            .Must(role => !string.IsNullOrWhiteSpace(role) && AllowedRoles.Contains(role.Trim()))
            .WithMessage("Unknown role.");
        RuleFor(command => command.TemporaryPassword)
            .MinimumLength(6)
            .When(command => !command.GeneratePassword)
            .WithMessage("Temporary password must be at least 6 characters.");
        RuleFor(command => command.TemporaryPassword)
            .NotEmpty()
            .When(command => !command.GeneratePassword)
            .WithMessage("Temporary password is required when password generation is disabled.");
        RuleFor(command => command.RequirePasswordChange)
            .Equal(toCompare: true)
            .When(command => command.SendCredentialsEmail)
            .WithMessage("A password change must be required when credentials are sent by email.");
    }
}
