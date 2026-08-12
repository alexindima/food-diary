using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserAuthenticationRegistrationService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService,
    IPasswordHasher passwordHasher) : IUserAuthenticationRegistrationService {
    public async Task<Result<UserAuthenticationPrincipalModel>> RegisterAsync(
        UserRegistrationModel registration,
        CancellationToken cancellationToken = default) {
        User? existingUser = await userLookupRepository
            .GetByEmailIncludingDeletedAsync(registration.Email, cancellationToken)
            .ConfigureAwait(false);
        if (existingUser is not null) {
            return existingUser.DeletedAt is not null
                ? Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.AccountDeleted)
                : Result.Failure<UserAuthenticationPrincipalModel>(EmailAlreadyExists);
        }

        var user = User.Create(registration.Email, passwordHasher.Hash(registration.Password));
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        user.SetLanguage(LanguageCode.FromPreferred(registration.Language).Value);
        user.SetEmailConfirmationToken(new UserTokenIssue(
            passwordHasher.Hash(registration.EmailVerificationToken),
            registration.EmailVerificationExpiresAtUtc,
            registration.RegisteredAtUtc));
        user.RecordAuthenticationActivity(registration.RegisteredAtUtc);
        await userWriteRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, registration.RegisteredAtUtc));
    }

    public async Task<UserInitialAdminBootstrapModel> BootstrapInitialAdminAsync(
        string email,
        string password,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default) {
        string normalizedEmail = email.Trim();
        User? existingUser = await userLookupRepository
            .GetByEmailIncludingDeletedAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);
        if (existingUser is not null) {
            return new UserInitialAdminBootstrapModel(Created: false, normalizedEmail);
        }

        IReadOnlyList<Role> roleEntities = await roleCatalogService
            .EnsureRolesByNamesAsync([.. roles], cancellationToken)
            .ConfigureAwait(false);
        var admin = User.Create(normalizedEmail, passwordHasher.Hash(password));
        admin.SetEmailConfirmed(isConfirmed: true);
        admin.ReplaceRoles(roleEntities);
        await userWriteRepository.AddAsync(admin, cancellationToken).ConfigureAwait(false);
        return new UserInitialAdminBootstrapModel(Created: true, normalizedEmail);
    }

    private static Error EmailAlreadyExists => new(
        "Validation.Conflict",
        "User with this email already exists.",
        Kind: ErrorKind.Conflict,
        Details: new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["Email"] = ["User with this email already exists."],
        });
}
