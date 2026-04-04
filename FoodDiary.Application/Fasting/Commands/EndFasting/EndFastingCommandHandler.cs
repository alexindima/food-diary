using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.EndFasting;

public class EndFastingCommandHandler(
    IFastingSessionRepository fastingRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EndFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        EndFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var current = await fastingRepository.GetCurrentAsync(userId, cancellationToken);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        current.End(dateTimeProvider.UtcNow);
        await fastingRepository.UpdateAsync(current, cancellationToken);

        return Result.Success(current.ToModel());
    }
}
