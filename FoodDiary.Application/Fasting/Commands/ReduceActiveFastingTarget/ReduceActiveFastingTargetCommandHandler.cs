using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;

public sealed class ReduceActiveFastingTargetCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReduceActiveFastingTargetCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        ReduceActiveFastingTargetCommand command,
        CancellationToken cancellationToken) {
        var userId = new UserId(command.UserId!.Value);
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
                Errors.Validation.Invalid(nameof(command.ReducedHours), "Only extended fasting target can be reduced."));
        }

        try {
            current.Reduce(command.ReducedHours);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<FastingSessionModel>(
                Errors.Validation.Invalid(nameof(command.ReducedHours), "Reduced fasting hours are invalid."));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        var now = dateTimeProvider.UtcNow;
        if (current.TargetHours.HasValue && now >= current.StartedAtUtc.AddHours(current.TargetHours.Value)) {
            current.Complete(now);
            plan.Stop(now);
            await fastingPlanRepository.UpdateAsync(plan, cancellationToken);
        }

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(current.ToModel(plan));
    }
}
