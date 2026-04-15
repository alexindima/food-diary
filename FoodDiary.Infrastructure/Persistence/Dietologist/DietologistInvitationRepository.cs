using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dietologist;

public class DietologistInvitationRepository(FoodDiaryDbContext context) : IDietologistInvitationRepository {
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
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
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
            .FirstOrDefaultAsync(i => i.ClientUserId == clientUserId && i.Status == status, cancellationToken);
    }

    public async Task<DietologistInvitation?> GetActiveByClientAsync(
        UserId clientUserId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        return await GetByClientAndStatusAsync(clientUserId, DietologistInvitationStatus.Accepted, asTracking, cancellationToken);
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
                cancellationToken);
    }

    public async Task<IReadOnlyList<DietologistInvitation>> GetActiveByDietologistAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await context.DietologistInvitations
            .AsNoTracking()
            .Include(i => i.ClientUser)
            .Where(i => i.DietologistUserId == dietologistUserId && i.Status == DietologistInvitationStatus.Accepted)
            .OrderByDescending(i => i.AcceptedAtUtc)
            .ToListAsync(cancellationToken);
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
                cancellationToken);
    }

    public async Task<DietologistInvitation> AddAsync(
        DietologistInvitation invitation, CancellationToken cancellationToken = default) {
        context.DietologistInvitations.Add(invitation);
        await context.SaveChangesAsync(cancellationToken);
        return invitation;
    }

    public async Task UpdateAsync(
        DietologistInvitation invitation, CancellationToken cancellationToken = default) {
        var entry = context.Entry(invitation);
        if (entry.State == EntityState.Detached) {
            context.Attach(invitation);
            entry.State = EntityState.Modified;

            if (invitation.ClientUser is not null) {
                context.Entry(invitation.ClientUser).State = EntityState.Unchanged;
            }

            if (invitation.DietologistUser is not null) {
                context.Entry(invitation.DietologistUser).State = EntityState.Unchanged;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
