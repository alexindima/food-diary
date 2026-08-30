using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

internal sealed class DietologistUserContextService(
    ICurrentUserAccessService currentUserAccessService,
    IUserDietologistProfileReadService profileReadService,
    IUserProfileReadService userProfileReadService,
    IDietologistUserLookupService userLookupService) : IDietologistUserContextService {
    public async Task<Result<string>> GetAccessibleUserEmailAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserDietologistProfileModel> profileResult = await profileReadService.GetAccessibleProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        return profileResult.IsFailure
            ? Result.Failure<string>(profileResult.Error)
            : Result.Success(profileResult.Value.Email);
    }

    public async Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken) {
        UserDietologistProfileModel? profile = await userLookupService.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return profile?.Email;
    }

    public async Task<Result<UserModel>> GetUserModelByIdAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserModel> userResult = await userProfileReadService.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure ? Result.Failure<UserModel>(Errors.Dietologist.AccessDenied) : userResult;
    }

    public Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken) =>
        profileReadService.GetAccessibleProfileAsync(userId, cancellationToken);

    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken);

    public Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        userLookupService.FindByEmailAsync(email, cancellationToken);

    public Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        userLookupService.FindByIdAsync(userId, cancellationToken);
}
