using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;

public class UpdateCurrentFastingCheckInCommandHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCurrentFastingCheckInCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        UpdateCurrentFastingCheckInCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        FastingCheckIn checkIn;
        try {
            var checkedInAtUtc = dateTimeProvider.UtcNow;
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

        await fastingCheckInRepository.AddAsync(checkIn, cancellationToken);
        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(current.ToModel(current.Plan, [checkIn]));
    }
}
