using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Services;

internal sealed class DietologistUserContextService(
    ICurrentUserAccessService currentUserAccessService,
    IUserDietologistProfileReadService profileReadService,
    IUserProfileReadService userProfileReadService) : IDietologistUserContextService {
    public async Task<Result<string>> GetAccessibleUserEmailAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserDietologistProfileModel> profileResult = await profileReadService.GetAccessibleProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<string>(profileResult.Error);
        }
        return profileResult.Value.Email is { } email
            ? Result.Success(email)
            : Result.Failure<string>(UserErrors.EmailRequired);
    }

    public async Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken) {
        UserDietologistProfileModel? profile = await profileReadService.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return profile?.Email;
    }

    public async Task<Result<UserModel>> GetUserModelByIdAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserModel> userResult = await userProfileReadService.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure ? Result.Failure<UserModel>(DietologistErrors.AccessDenied) : userResult;
    }

    public Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken) =>
        profileReadService.GetAccessibleProfileAsync(userId, cancellationToken);

    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken);

    public Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        profileReadService.FindByEmailAsync(email, cancellationToken);

    public Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        profileReadService.FindByIdAsync(userId, cancellationToken);
}
