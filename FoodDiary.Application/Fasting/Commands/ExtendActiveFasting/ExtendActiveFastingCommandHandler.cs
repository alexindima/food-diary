using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;

public class ExtendActiveFastingCommandHandler(IFastingSessionRepository fastingRepository)
    : ICommandHandler<ExtendActiveFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        ExtendActiveFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var current = await fastingRepository.GetCurrentAsync(userId, cancellationToken);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        try {
            current.Extend(command.AdditionalHours);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<FastingSessionModel>(
                Errors.Validation.Invalid(nameof(command.AdditionalHours), "Additional fasting hours are invalid."));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        await fastingRepository.UpdateAsync(current, cancellationToken);
        return Result.Success(current.ToModel());
    }
}
