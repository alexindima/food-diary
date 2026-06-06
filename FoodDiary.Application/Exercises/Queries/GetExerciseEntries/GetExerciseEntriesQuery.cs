using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Exercises.Models;

namespace FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

public record GetExerciseEntriesQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo) : IQuery<Result<IReadOnlyList<ExerciseEntryModel>>>, IUserRequest;
