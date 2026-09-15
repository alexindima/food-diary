using FoodDiary.Modules.Exercises.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseCalories;

namespace FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseCalories;

public sealed class ReadExerciseCaloriesQueryHandler(IExerciseEntryReadRepository exerciseEntryReadRepository) : IQueryHandler<ReadExerciseCaloriesQuery, double> {
    public Task<double> Handle(ReadExerciseCaloriesQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime dateUtc = request.DateUtc;
        return exerciseEntryReadRepository.GetTotalCaloriesBurnedAsync(userId, dateUtc, cancellationToken);
    }

}
