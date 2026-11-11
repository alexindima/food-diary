using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Enums;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

internal sealed class UserTelegramDiaryTransfer(
    UsersDbContext context,
    IModuleTransactionCoordinator coordinator,
    IUnitOfWork unitOfWork,
    IUserLookupRepository users,
    ITelegramDiaryTransferReader reader,
    IEnumerable<ITelegramDiaryContentTransfer> content,
    IUserSessionRevocationService sessions,
    TimeProvider timeProvider) : IUserTelegramDiaryTransfer {
    public async Task<TelegramDiaryTarget?> FindTargetAsync(string verifiedEmail, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        return await context.Users.AsNoTracking()
            .Where(user => user.Email == verifiedEmail && user.IsEmailConfirmed && user.IsActive && user.DeletedAt == null)
            .Select(user => new TelegramDiaryTarget(user.Id, user.SecurityVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<TelegramDiaryPreview>> TransferAsync(
        TelegramDiaryProof proof, bool? useTelegramGoals, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(
            coordinator.CurrentTransaction ?? throw new InvalidOperationException("Diary transfer requires a caller transaction."),
            cancellationToken);
        // Stable lock order fences competing ownership changes and FK-backed inserts for both users.
        _ = await context.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value"
            FROM "Users"
            WHERE "Id" IN ({proof.SourceId.Value}, {proof.TargetId.Value})
            ORDER BY "Id"
            FOR UPDATE
            """).ToArrayAsync(cancellationToken);

        User? source = await users.GetByIdAsync(proof.SourceId, cancellationToken);
        User? target = await users.GetByIdAsync(proof.TargetId, cancellationToken);
        if (source is null || target is null || source.Id == target.Id || source.SecurityVersion != proof.SourceSecurityVersion ||
            target.SecurityVersion != proof.TargetSecurityVersion || !target.IsEmailConfirmed ||
            !string.Equals(target.Email, proof.VerifiedEmail, StringComparison.Ordinal)) {
            return Failure("Authentication.TelegramDiaryProofExpired", "Start a new diary transfer.");
        }
        bool confirmedAdmin = proof.AdminTargetConfirmed && target.HasRole(RoleNames.Admin) &&
            !target.HasRole(RoleNames.Support) && !target.HasRole(RoleNames.Dietologist);
        if (IsPrivileged(source) || (IsPrivileged(target) && !confirmedAdmin)) {
            return Failure("User.TelegramDiaryRestrictedAccount", "Automatic diary transfer is unavailable for accounts with privileged roles.");
        }

        if (source.TelegramUserId is not { } telegramId || source.Email is not null || source.HasPassword ||
            source.GoogleSubject is not null || target.TelegramUserId.HasValue || target.MustChangePassword || source.HasRole(RoleNames.Premium) ||
            source.ManualPremiumEndsAtUtc.HasValue || source.PremiumAccessEndsAtUtc.HasValue) {
            return Failure("User.TelegramDiaryTransferUnavailable", "These accounts cannot be combined.");
        }

        if (source.WeightGoals.Count > 0 || source.WaistGoals.Count > 0) {
            return Failure("User.TelegramDiaryTransferUnavailable", "The source has tracked body goals that cannot be moved.");
        }

        Result<TelegramDiaryCounts> records = await reader.ReadAsync(source.Id, target.Id, cancellationToken);
        if (records.IsFailure) {
            return Result.Failure<TelegramDiaryPreview>(records.Error);
        }
        bool canUseTelegramGoals = !HasTrackedBodyGoals(target);
        var preview = new TelegramDiaryPreview(records.Value, Goals(target), Goals(source), canUseTelegramGoals);
        if (useTelegramGoals is null) {
            return Result.Success(preview);
        }
        if (useTelegramGoals.Value && !canUseTelegramGoals) {
            return Failure("User.TelegramDiaryGoalsUnavailable", "Keep the existing account's goals to preserve tracked body goals.");
        }
        if ((records.Value.Meals > 0 || records.Value.WaterEntries > 0 || records.Value.Measurements > 0 || useTelegramGoals.Value) &&
            (target.HealthConsentAcceptedAtUtc is null ||
                !string.Equals(target.HealthConsentNoticeVersion, PrivacyNoticeVersions.Current, StringComparison.Ordinal))) {
            return Result.Failure<TelegramDiaryPreview>(new Error("Privacy.HealthConsentRequired",
                "Review your health data processing choices in privacy settings.", ErrorKind.Forbidden));
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        string? issuer = source.TelegramOidcIssuer;
        string? subject = source.TelegramOidcSubject;
        foreach (ITelegramDiaryContentTransfer participant in content.OrderBy(item => item.Order)) {
            await participant.TransferAsync(source.Id, target.Id, cancellationToken);
        }
        source.RetireTelegramDiary(now);
        await sessions.RevokeAllAsync(source.Id, now, cancellationToken);
        // Release both unique Telegram keys before assigning them to the target, within the same transaction.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        target.LinkTelegram(telegramId);
        if (issuer is not null && subject is not null) {
            target.BindTelegramOidcIdentity(issuer, subject);
        }
        if (useTelegramGoals.Value) {
            target.AdoptDiaryGoals(source);
        }
        target.AdoptDiaryPremiumTrial(source);
        await sessions.RevokeAllAsync(target.Id, now, cancellationToken);
        return Result.Success(preview);
    }

    private static bool HasTrackedBodyGoals(User user) =>
        user.WeightGoals.Any(goal => goal.Status == WeightGoalStatus.Active) ||
        user.WaistGoals.Any(goal => goal.Status == WaistGoalStatus.Active);

    private static bool IsPrivileged(User user) => user.HasRole(RoleNames.Owner) || user.HasRole(RoleNames.Admin) ||
        user.HasRole(RoleNames.Support) || user.HasRole(RoleNames.Dietologist);

    private static TelegramDiaryGoals Goals(User user) => new(user.DailyCalorieTarget, user.ProteinTarget,
        user.FatTarget, user.CarbTarget, user.FiberTarget, user.HydrationGoal ?? user.WaterGoal,
        user.StepGoal, user.DesiredWeightKg, user.DesiredWaistCm);

    private static Result<TelegramDiaryPreview> Failure(string code, string message) =>
        Result.Failure<TelegramDiaryPreview>(new Error(code, message, ErrorKind.Conflict));
}
