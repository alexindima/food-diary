using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IDietologistInvitationWriteRepository : IDietologistInvitationReadRepository {
    Task<DietologistInvitation> AddAsync(DietologistInvitation invitation, CancellationToken cancellationToken = default);

    Task UpdateAsync(DietologistInvitation invitation, CancellationToken cancellationToken = default);
}
