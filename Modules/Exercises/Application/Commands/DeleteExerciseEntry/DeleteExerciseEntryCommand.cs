using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;

public record DeleteExerciseEntryCommand(
    Guid? UserId,
    Guid EntryId) : ICommand<Result>, IUserRequest;
