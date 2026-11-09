using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Commands.StartUserPremiumTrial;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Commands.StartUserPremiumTrial;

public sealed class StartUserPremiumTrialCommandHandler(IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository, IUserPremiumTrialStore trials) : IRequestHandler<StartUserPremiumTrialCommand, Result<UserBillingProfileModel>> {
    public async Task<Result<UserBillingProfileModel>> Handle(StartUserPremiumTrialCommand request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime startedAtUtc = request.StartedAtUtc;
        TimeSpan duration = request.Duration;
        await trials.LockAccountAsync(userId, cancellationToken);
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (error is not null) {
            return Result.Failure<UserBillingProfileModel>(error);
        }

        if (!string.IsNullOrWhiteSpace(user!.Email) && !user.IsEmailConfirmed) {
            return Result.Failure<UserBillingProfileModel>(UserErrors.EmailRequired);
        }
        if (user.HasUsedPremiumTrial() || user.PremiumTrialEndsAtUtc.HasValue || user.PremiumTrialRestrictedByTelegram) {
            return Result.Failure<UserBillingProfileModel>(UserErrors.PremiumTrialAlreadyUsed);
        }
        if (user.TelegramUserId is { } telegramId) {
            if (!user.TelegramLinkVerifiedForRecovery) {
                return Result.Failure<UserBillingProfileModel>(UserErrors.TelegramTrialLinkUnverified);
            }
            if (!await trials.TryRedeemTelegramAsync(telegramId, cancellationToken)) {
                return Result.Failure<UserBillingProfileModel>(UserErrors.PremiumTrialAlreadyUsed);
            }
        }

        user.StartPremiumTrial(startedAtUtc, duration);
        await userWriteRepository.UpdateAsync(user, cancellationToken);
        return Result.Success(ToModel(user));

    }

    private static UserBillingProfileModel ToModel(User user) =>
        new(
            user.Id,
            user.Email,
            user.IsActive,
            user.DeletedAt is not null,
            user.HasRole(RoleNames.Premium),
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc,
            user.IsEmailConfirmed,
            TelegramUserId: user.TelegramUserId,
            HasVerifiedTelegramLink: user.TelegramLinkVerifiedForRecovery);

}
