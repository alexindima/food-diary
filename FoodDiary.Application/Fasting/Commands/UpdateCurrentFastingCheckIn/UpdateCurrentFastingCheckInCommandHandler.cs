using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;

public sealed class UpdateCurrentFastingCheckInCommandHandler(
    IFastingOccurrenceWriteRepository fastingOccurrenceRepository,
    IFastingCheckInWriteRepository fastingCheckInRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : ICommandHandler<UpdateCurrentFastingCheckInCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        UpdateCurrentFastingCheckInCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await FastingCurrentUserResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return FastingCurrentUserResolver.ToSessionFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        FastingOccurrence? current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        FastingCheckIn checkIn;
        try {
            DateTime checkedInAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
            checkIn = FastingCheckIn.Create(
                current.Id,
                userId,
                command.HungerLevel,
                command.EnergyLevel,
                command.MoodLevel,
                command.Symptoms,
                command.CheckInNotes,
                checkedInAtUtc);
            current.UpdateCheckIn(
                command.HungerLevel,
                command.EnergyLevel,
                command.MoodLevel,
                command.Symptoms,
                command.CheckInNotes,
                checkedInAtUtc);
        } catch (ArgumentOutOfRangeException ex) {
            return Result.Failure<FastingSessionModel>(Errors.Validation.Invalid("CheckIn", ex.Message));
        }

        await fastingCheckInRepository.AddAsync(checkIn, cancellationToken).ConfigureAwait(false);
        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);

        return Result.Success(current.ToModel(current.Plan, [checkIn]));
    }
}
