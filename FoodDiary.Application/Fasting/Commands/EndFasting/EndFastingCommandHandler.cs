using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.EndFasting;

public class EndFastingCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EndFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        EndFastingCommand command, CancellationToken cancellationToken) {
        var userId = new UserId(command.UserId!.Value);
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        var plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        var now = dateTimeProvider.UtcNow;
        if (plan.Type == FastingPlanType.Cyclic) {
            current.Interrupt(now);
            plan.Stop(now);
            await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
            await fastingPlanRepository.UpdateAsync(plan, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(current.ToModel(plan));
        }

        if (plan.Type == FastingPlanType.Intermittent) {
            current.Complete(now);
        } else {
            var targetReachedAtUtc = current.StartedAtUtc.AddHours(current.TargetHours ?? 0);
            if (now >= targetReachedAtUtc) {
                current.Complete(now);
            } else {
                current.Interrupt(now);
            }
        }

        plan.Stop(now);
        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(current.ToModel(plan));
    }
}
