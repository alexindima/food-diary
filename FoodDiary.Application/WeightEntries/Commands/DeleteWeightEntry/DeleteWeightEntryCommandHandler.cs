using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public class DeleteWeightEntryCommandHandler(
    IWeightEntryRepository weightEntryRepository,
    IUserRepository userRepository)
    : ICommandHandler<DeleteWeightEntryCommand, Result> {
    public async Task<Result> Handle(DeleteWeightEntryCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.WeightEntryId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.WeightEntryId), "Weight entry id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var weightEntryId = new WeightEntryId(command.WeightEntryId);
        var entry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (entry is null) {
            return Result.Failure(Errors.WeightEntry.NotFound(command.WeightEntryId));
        }

        await weightEntryRepository.DeleteAsync(entry, cancellationToken);
        return Result.Success();
    }
}
