using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;

public class UpdateCurrentFastingCheckInCommandHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCurrentFastingCheckInCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        UpdateCurrentFastingCheckInCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingSessionModel>(accessError);
        }

        FastingOccurrence? current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        FastingCheckIn checkIn;
        try {
            DateTime checkedInAtUtc = dateTimeProvider.UtcNow;
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
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(current.ToModel(current.Plan, [checkIn]));
    }
}
