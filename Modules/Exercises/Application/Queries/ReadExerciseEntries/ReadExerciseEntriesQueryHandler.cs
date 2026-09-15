using FoodDiary.Modules.Exercises.Application.Abstractions.Common;
using FoodDiary.Modules.Exercises.Application.Abstractions.Models;
using FoodDiary.Modules.Exercises.Application.Mappings;
using FoodDiary.Modules.Exercises.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries;

namespace FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseEntries;

public sealed class ReadExerciseEntriesQueryHandler(IExerciseEntryReadModelRepository exerciseEntryReadModelRepository) : IQueryHandler<ReadExerciseEntriesQuery, IReadOnlyList<ExerciseEntryModel>> {
    public async Task<IReadOnlyList<ExerciseEntryModel>> Handle(ReadExerciseEntriesQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime dateFrom = request.DateFrom;
        DateTime dateTo = request.DateTo;
        IReadOnlyList<ExerciseEntryReadModel> entries = await exerciseEntryReadModelRepository
            .GetByDateRangeReadModelsAsync(userId, dateFrom, dateTo, cancellationToken)
            .ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }

}
