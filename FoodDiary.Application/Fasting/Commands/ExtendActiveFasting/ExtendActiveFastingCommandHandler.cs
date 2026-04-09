using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;

public class ExtendActiveFastingCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ExtendActiveFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        ExtendActiveFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        var plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        if (plan.Type != FastingPlanType.Extended) {
            return Result.Failure<FastingSessionModel>(
                Errors.Validation.Invalid(nameof(command.AdditionalHours), "Only extended fasting can be extended."));
        }

        try {
            current.Extend(command.AdditionalHours);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<FastingSessionModel>(
                Errors.Validation.Invalid(nameof(command.AdditionalHours), "Additional fasting hours are invalid."));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(current.ToModel(plan));
    }
}
