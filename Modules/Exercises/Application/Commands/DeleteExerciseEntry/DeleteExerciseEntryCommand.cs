using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Exercises.Application.Commands.DeleteExerciseEntry;

public record DeleteExerciseEntryCommand(
    Guid? UserId,
    Guid EntryId) : ICommand<Result>, IUserRequest;
