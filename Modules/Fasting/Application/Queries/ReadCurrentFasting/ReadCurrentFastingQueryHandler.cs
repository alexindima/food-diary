using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Fasting.Application.Mappings;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadCurrentFasting;

namespace FoodDiary.Modules.Fasting.Application.Queries.ReadCurrentFasting;

public sealed class ReadCurrentFastingQueryHandler(IFastingOccurrenceReadModelRepository fastingOccurrenceRepository,
    IFastingCheckInReadModelRepository fastingCheckInRepository) : IQueryHandler<ReadCurrentFastingQuery, FastingSessionModel?> {
    public async Task<FastingSessionModel?> Handle(ReadCurrentFastingQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        FastingOccurrenceReadModel? current = await GetCurrentOccurrenceAsync(userId, cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return null;
        }

        IReadOnlyList<FastingCheckInReadModel> checkIns = await GetCheckInsAsync(current, cancellationToken).ConfigureAwait(false);
        return current.ToModel(current.Plan, checkIns);
    }

    private Task<FastingOccurrenceReadModel?> GetCurrentOccurrenceAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        fastingOccurrenceRepository.GetCurrentReadModelAsync(userId, cancellationToken);

    private Task<IReadOnlyList<FastingCheckInReadModel>> GetCheckInsAsync(
        FastingOccurrenceReadModel occurrence,
        CancellationToken cancellationToken) =>
        fastingCheckInRepository.GetByOccurrenceIdReadModelsAsync([occurrence.Id], cancellationToken);

}
