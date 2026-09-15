using FoodDiary.Modules.Exercises.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record ReadExerciseEntriesQuery(UserId UserId, DateTime DateFrom, DateTime DateTo) : IQuery<IReadOnlyList<ExerciseEntryModel>>;
