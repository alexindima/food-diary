using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

public class StartFastingCommandHandler(
    IFastingSessionRepository fastingRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<StartFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        StartFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<FastingSessionModel>(accessError);
        }

        var current = await fastingRepository.GetCurrentAsync(userId, cancellationToken);
        if (current is not null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.AlreadyActive);
        }

        if (!Enum.TryParse<FastingProtocol>(command.Protocol, ignoreCase: true, out var protocol)) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidProtocol);
        }

        var duration = command.PlannedDurationHours ?? FastingSession.GetDefaultDuration(protocol);

        var session = FastingSession.Create(userId, protocol, duration, dateTimeProvider.UtcNow, command.Notes);
        await fastingRepository.AddAsync(session, cancellationToken);

        return Result.Success(session.ToModel());
    }
}
