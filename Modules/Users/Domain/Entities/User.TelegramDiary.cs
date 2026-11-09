using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Entities;

public sealed partial class User {
    public void RetireTelegramDiary(DateTime deletedAtUtc) {
        EnsureNotDeleted();
        if (!TelegramUserId.HasValue || Email is not null || HasPassword || GoogleSubject is not null) {
            throw new InvalidOperationException("Only a Telegram-only account can be retired by a diary transfer.");
        }

        ApplyAccountState(GetAccountState().WithTelegram(null));
        TelegramLinkVerifiedForRecovery = false;
        TelegramOidcIssuer = null;
        TelegramOidcSubject = null;
        MarkDeleted(deletedAtUtc);
    }

    public void AdoptDiaryGoals(User source) {
        EnsureNotDeleted();
        UserDailyGoals previous = NutritionProfile.GetDailyGoals();
        ApplyGoalState(source.GetGoalState());
        StepGoal = source.StepGoal;
        HydrationGoal = source.HydrationGoal;
        RecordGoalChanges(previous);
        NutritionProfile.Touch();
    }

    public void AdoptDiaryPremiumTrial(User source) {
        EnsureNotDeleted();
        if (source.PremiumTrialRestrictedByTelegram && !PremiumTrialRestrictedByTelegram) {
            PremiumTrialRestrictedByTelegram = true;
            SetModified();
        }
        if (!HasUsedPremiumTrial() && source.PremiumTrialStartedAtUtc is { } start &&
            source.PremiumTrialEndsAtUtc is { } end && end > start) {
            StartPremiumTrial(start, end - start);
        }
    }
}
