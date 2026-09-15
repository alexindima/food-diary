using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseCalories;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadExerciseCaloriesQuery(UserId UserId, DateTime DateUtc) : IQuery<double>;
