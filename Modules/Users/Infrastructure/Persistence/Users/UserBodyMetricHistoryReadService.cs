using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserBodyMetricHistoryReadService(
    DbSet<User> users, DbSet<WeightGoal> weights, DbSet<WaistGoal> waists,
    Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserBodyMetricHistoryReadService {
    private IQueryable<User> AccessibleUsers => users.AsNoTracking().Where(user => user.IsActive && user.DeletedAt == null);

    private async Task<bool> CanReadAsync(UserId userId, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await AccessibleUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<WeightHistoryProfileModel>> GetWeightHistoryProfileAsync(UserId userId, CancellationToken cancellationToken) {
        if (!await CanReadAsync(userId, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<WeightHistoryProfileModel>(AuthenticationErrors.InvalidToken);
        }
        var profile = await AccessibleUsers.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new { user.HeightCm, user.DesiredWeightKg }).SingleAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<WeightGoal> owned = weights.AsNoTracking().Where(goal => goal.UserId == userId);
        List<WeightGoalHistoryModel> active = await ProjectWeight(owned.Where(goal => goal.Status == WeightGoalStatus.Active).Take(1))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        List<WeightGoalHistoryModel> latest = await ProjectWeight(owned.Where(goal => goal.Status != WeightGoalStatus.Active)
            .OrderByDescending(goal => goal.StartedAtUtc).ThenByDescending(goal => goal.Id).Take(1))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        WeightGoalHistoryModel? current = active.SingleOrDefault();
        return Result.Success(new WeightHistoryProfileModel(profile.HeightCm,
            new UserDesiredWeightModel(profile.DesiredWeightKg, current?.StartWeightKg, current?.StartedAtUtc), [.. active, .. latest]));
    }

    public async Task<Result<IReadOnlyList<WeightGoalHistoryModel>>> ReadWeightGoalsAsync(
        UserId userId, DateTime snapshotUtc, int offset, int limit, CancellationToken cancellationToken) {
        if (!await CanReadAsync(userId, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<IReadOnlyList<WeightGoalHistoryModel>>(AuthenticationErrors.InvalidToken);
        }
        List<WeightGoalHistoryModel> items = await ProjectWeight(weights.AsNoTracking()
            .Where(goal => goal.UserId == userId && goal.Status != WeightGoalStatus.Active && goal.EndedAtUtc <= snapshotUtc)
            .OrderByDescending(goal => goal.StartedAtUtc).ThenByDescending(goal => goal.Id)
            .Skip(offset).Take(limit)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WeightGoalHistoryModel>>(items);
    }

    private static IQueryable<WeightGoalHistoryModel> ProjectWeight(IQueryable<WeightGoal> query) =>
        query.AsNoTracking().Select(goal => new WeightGoalHistoryModel(goal.Id.Value, goal.TargetWeightKg, goal.StartWeightKg, goal.EndWeightKg,
            goal.StartedAtUtc, goal.EndedAtUtc, goal.Status.ToString()));

    public async Task<Result<WaistHistoryProfileModel>> GetWaistHistoryProfileAsync(UserId userId, CancellationToken cancellationToken) {
        if (!await CanReadAsync(userId, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<WaistHistoryProfileModel>(AuthenticationErrors.InvalidToken);
        }
        var profile = await AccessibleUsers.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new { user.HeightCm, user.DesiredWaistCm }).SingleAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<WaistGoal> owned = waists.AsNoTracking().Where(goal => goal.UserId == userId);
        List<WaistGoalHistoryModel> active = await ProjectWaist(owned.Where(goal => goal.Status == WaistGoalStatus.Active).Take(1))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        List<WaistGoalHistoryModel> latest = await ProjectWaist(owned.Where(goal => goal.Status != WaistGoalStatus.Active)
            .OrderByDescending(goal => goal.StartedAtUtc).ThenByDescending(goal => goal.Id).Take(1))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        WaistGoalHistoryModel? current = active.SingleOrDefault();
        return Result.Success(new WaistHistoryProfileModel(profile.HeightCm,
            new UserDesiredWaistModel(profile.DesiredWaistCm, current?.StartWaistCm, current?.StartedAtUtc), [.. active, .. latest]));
    }

    public async Task<Result<IReadOnlyList<WaistGoalHistoryModel>>> ReadWaistGoalsAsync(
        UserId userId, DateTime snapshotUtc, int offset, int limit, CancellationToken cancellationToken) {
        if (!await CanReadAsync(userId, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<IReadOnlyList<WaistGoalHistoryModel>>(AuthenticationErrors.InvalidToken);
        }
        List<WaistGoalHistoryModel> items = await ProjectWaist(waists.AsNoTracking()
            .Where(goal => goal.UserId == userId && goal.Status != WaistGoalStatus.Active && goal.EndedAtUtc <= snapshotUtc)
            .OrderByDescending(goal => goal.StartedAtUtc).ThenByDescending(goal => goal.Id)
            .Skip(offset).Take(limit)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WaistGoalHistoryModel>>(items);
    }

    private static IQueryable<WaistGoalHistoryModel> ProjectWaist(IQueryable<WaistGoal> query) =>
        query.AsNoTracking().Select(goal => new WaistGoalHistoryModel(goal.Id.Value, goal.TargetWaistCm, goal.StartWaistCm, goal.EndWaistCm,
            goal.StartedAtUtc, goal.EndedAtUtc, goal.Status.ToString()));
}
