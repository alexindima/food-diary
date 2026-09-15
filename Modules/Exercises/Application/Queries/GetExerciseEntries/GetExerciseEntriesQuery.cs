using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Exercises.Contracts.Models;

namespace FoodDiary.Modules.Exercises.Application.Queries.GetExerciseEntries;

public record GetExerciseEntriesQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo) : IQuery<Result<IReadOnlyList<ExerciseEntryModel>>>, IUserRequest;
