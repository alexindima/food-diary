using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;

public record DeleteExerciseEntryCommand(
    Guid? UserId,
    Guid EntryId) : ICommand<Result>, IUserRequest;
