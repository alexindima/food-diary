using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

internal sealed class ClientTaskRepository(FoodDiaryDbContext context) : IClientTaskRepository {
    public async Task<ClientTask> AddAsync(ClientTask task, CancellationToken cancellationToken = default) {
        await context.ClientTasks.AddAsync(task, cancellationToken).ConfigureAwait(false);
        return task;
    }

    public Task<ClientTask?> GetByIdAsync(
        ClientTaskId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<ClientTask> query = context.ClientTasks;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(
        UserId clientUserId,
        CancellationToken cancellationToken = default) =>
        await Project(context.ClientTasks.Where(task => task.ClientUserId == clientUserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default) =>
        await Project(context.ClientTasks.Where(task =>
                task.DietologistUserId == dietologistUserId &&
                task.ClientUserId == clientUserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ClientTask>> GetDueForReminderAsync(
        DateTime utcNow,
        DateTime dueBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default) =>
        await context.ClientTasks
            .Where(task =>
                task.Status == FoodDiary.Domain.Enums.ClientTaskStatus.Open &&
                task.DueAtUtc >= utcNow &&
                task.DueAtUtc <= dueBeforeUtc &&
                task.DueReminderSentAtUtc == null)
            .OrderBy(task => task.DueAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private static IQueryable<ClientTaskReadModel> Project(IQueryable<ClientTask> query) =>
        query
            .AsNoTracking()
            .OrderByDescending(task => task.CreatedOnUtc)
            .Select(task => new ClientTaskReadModel(
                task.Id.Value,
                task.DietologistUserId.Value,
                task.ClientUserId.Value,
                task.Title,
                task.Details,
                task.DueAtUtc,
                task.Status,
                task.CreatedOnUtc,
                task.StatusChangedAtUtc));
}
