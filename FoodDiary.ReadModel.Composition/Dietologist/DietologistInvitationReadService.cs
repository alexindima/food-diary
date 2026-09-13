using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Dietologist;

internal sealed class DietologistInvitationReadService(FoodDiaryDbContext context) : IDietologistInvitationReadModelRepository {
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

    public async Task<IReadOnlyList<DietologistInvitationReadModel>> GetActiveByDietologistReadModelsAsync(
        UserId dietologistUserId,
        CancellationToken cancellationToken = default) {
        return await Project(context.DietologistInvitations
            .AsNoTracking()
            .Where(i => i.DietologistUserId == dietologistUserId && i.Status == DietologistInvitationStatus.Accepted)
            .OrderByDescending(i => i.AcceptedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
