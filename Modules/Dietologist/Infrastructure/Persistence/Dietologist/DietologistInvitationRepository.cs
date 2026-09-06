using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Infrastructure.Persistence.Dietologist;

public sealed class DietologistInvitationRepository(FoodDiaryDbContext context) : IDietologistInvitationRepository {
    private IQueryable<DietologistInvitationReadModel> Project(IQueryable<DietologistInvitation> query) =>
        from invitation in query
        join client in context.Users.AsNoTracking() on invitation.ClientUserId equals client.Id
        join candidate in context.Users.AsNoTracking() on invitation.DietologistUserId equals (UserId?)candidate.Id into dietologists
        from dietologist in dietologists.DefaultIfEmpty()
        select new DietologistInvitationReadModel(
            invitation.Id.Value,
            invitation.ClientUserId.Value,
            invitation.DietologistUserId.HasValue ? invitation.DietologistUserId.Value.Value : null,
            invitation.DietologistEmail,
            client.Email,
            client.FirstName,
            client.LastName,
            client.ProfileImage,
            client.BirthDate,
            client.Gender,
            client.HeightCm,
            client.ActivityLevel,
            dietologist == null ? null : dietologist.Email,
            dietologist == null ? null : dietologist.FirstName,
            dietologist == null ? null : dietologist.LastName,
            invitation.Status,
            new DietologistPermissionsReadModel(
                invitation.ShareMeals,
                invitation.ShareStatistics,
                invitation.ShareWeight,
                invitation.ShareWaist,
                invitation.ShareGoals,
                invitation.ShareHydration,
                invitation.ShareProfile,
                invitation.ShareFasting),
            invitation.CreatedOnUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc);

    public async Task<DietologistInvitationReadModel?> GetByIdReadModelAsync(
        DietologistInvitationId id,
        CancellationToken cancellationToken = default) {
        return await Project(context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.Id == id))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitation?> GetByIdAsync(
        DietologistInvitationId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<DietologistInvitation> query = context.DietologistInvitations;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitation?> GetByClientAndStatusAsync(
        UserId clientUserId,
        DietologistInvitationStatus status,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<DietologistInvitation> query = context.DietologistInvitations;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query
            .FirstOrDefaultAsync(i => i.ClientUserId == clientUserId && i.Status == status, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitationReadModel?> GetByClientAndStatusReadModelAsync(
        UserId clientUserId,
        DietologistInvitationStatus status,
        CancellationToken cancellationToken = default) {
        return await Project(context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.ClientUserId == clientUserId && i.Status == status))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitationReadModel?> GetActiveByClientReadModelAsync(
        UserId clientUserId,
        CancellationToken cancellationToken = default) {
        return await GetByClientAndStatusReadModelAsync(
            clientUserId,
            DietologistInvitationStatus.Accepted,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitation?> GetActiveByClientAsync(
        UserId clientUserId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        return await GetByClientAndStatusAsync(clientUserId, DietologistInvitationStatus.Accepted, asTracking, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitationReadModel?> GetActiveByClientAndDietologistReadModelAsync(
        UserId clientUserId,
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await Project(context.DietologistInvitations
            .AsNoTracking()
            .Where(i =>
                i.ClientUserId == clientUserId
                && i.DietologistUserId == dietologistUserId
                && i.Status == DietologistInvitationStatus.Accepted))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
        UserId clientUserId,
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.ClientUserId == clientUserId
                && i.DietologistUserId == dietologistUserId
                && i.Status == DietologistInvitationStatus.Accepted,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DietologistInvitationReadModel>> GetActiveByDietologistReadModelsAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await Project(context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.DietologistUserId == dietologistUserId && i.Status == DietologistInvitationStatus.Accepted)
            .OrderByDescending(i => i.AcceptedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DietologistInvitation>> GetActiveByDietologistAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.DietologistUserId == dietologistUserId && i.Status == DietologistInvitationStatus.Accepted)
            .OrderByDescending(i => i.AcceptedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasActiveRelationshipAsync(
        UserId clientUserId,
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AnyAsync(i =>
                i.ClientUserId == clientUserId
                && i.DietologistUserId == dietologistUserId
                && i.Status == DietologistInvitationStatus.Accepted,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitation> AddAsync(
        DietologistInvitation invitation, CancellationToken cancellationToken = default) {
        await context.DietologistInvitations.AddAsync(invitation, cancellationToken).ConfigureAwait(false);
        return invitation;
    }

    public async Task UpdateAsync(
        DietologistInvitation invitation, CancellationToken cancellationToken = default) {
        EntityEntry<DietologistInvitation> entry = context.Entry(invitation);
        if (entry.State == EntityState.Detached) {
            DietologistInvitation? existing = await context.DietologistInvitations
                .FirstOrDefaultAsync(i => i.Id == invitation.Id, cancellationToken).ConfigureAwait(false) ?? throw new DbUpdateConcurrencyException(
                    $"Dietologist invitation '{invitation.Id.Value}' was not found while updating.");
            context.Entry(existing).CurrentValues.SetValues(invitation);
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

}
