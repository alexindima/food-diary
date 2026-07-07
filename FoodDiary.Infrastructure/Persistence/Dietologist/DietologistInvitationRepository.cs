using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace FoodDiary.Infrastructure.Persistence.Dietologist;

public sealed class DietologistInvitationRepository(FoodDiaryDbContext context) : IDietologistInvitationRepository {
    private static readonly Expression<Func<DietologistInvitation, DietologistInvitationReadModel>> ReadModelProjection =
        invitation => new DietologistInvitationReadModel(
            invitation.Id.Value,
            invitation.ClientUserId.Value,
            invitation.DietologistUserId.HasValue ? invitation.DietologistUserId.Value.Value : null,
            invitation.DietologistEmail,
            invitation.ClientUser.Email,
            invitation.ClientUser.FirstName,
            invitation.ClientUser.LastName,
            invitation.ClientUser.ProfileImage,
            invitation.ClientUser.BirthDate,
            invitation.ClientUser.Gender,
            invitation.ClientUser.Height,
            invitation.ClientUser.ActivityLevel,
            invitation.DietologistUser == null ? null : invitation.DietologistUser.Email,
            invitation.DietologistUser == null ? null : invitation.DietologistUser.FirstName,
            invitation.DietologistUser == null ? null : invitation.DietologistUser.LastName,
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
        return await context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(ReadModelProjection)
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
            .Include(i => i.ClientUser)
            .Include(i => i.DietologistUser)
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
            .Include(i => i.DietologistUser)
            .FirstOrDefaultAsync(i => i.ClientUserId == clientUserId && i.Status == status, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitationReadModel?> GetByClientAndStatusReadModelAsync(
        UserId clientUserId,
        DietologistInvitationStatus status,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.ClientUserId == clientUserId && i.Status == status)
            .Select(ReadModelProjection)
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
        return await context.DietologistInvitations
            .AsNoTracking()
            .Where(i =>
                i.ClientUserId == clientUserId
                && i.DietologistUserId == dietologistUserId
                && i.Status == DietologistInvitationStatus.Accepted)
            .Select(ReadModelProjection)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
        UserId clientUserId,
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AsNoTracking()
            .Include(i => i.ClientUser)
            .Include(i => i.DietologistUser)
            .FirstOrDefaultAsync(i =>
                i.ClientUserId == clientUserId
                && i.DietologistUserId == dietologistUserId
                && i.Status == DietologistInvitationStatus.Accepted,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DietologistInvitationReadModel>> GetActiveByDietologistReadModelsAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.DietologistUserId == dietologistUserId && i.Status == DietologistInvitationStatus.Accepted)
            .OrderByDescending(i => i.AcceptedAtUtc)
            .Select(ReadModelProjection)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DietologistInvitation>> GetActiveByDietologistAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AsNoTracking()
            .Include(i => i.ClientUser)
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
                .FirstOrDefaultAsync(i => i.Id == invitation.Id, cancellationToken).ConfigureAwait(false);

            if (existing is null) {
                throw new DbUpdateConcurrencyException(
                    $"Dietologist invitation '{invitation.Id.Value}' was not found while updating.");
            }

            context.Entry(existing).CurrentValues.SetValues(invitation);
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

}
