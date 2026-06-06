using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.EndFasting;

public class EndFastingCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IUserRepository userRepository,
    TimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EndFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        EndFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingSessionModel>(accessError);
        }

        FastingOccurrence? current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        FastingPlan? plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        if (plan.Type == FastingPlanType.Cyclic) {
            current.Interrupt(now);
            plan.Stop(now);
            await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
            await fastingPlanRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success(current.ToModel(plan));
        }

        if (plan.Type == FastingPlanType.Intermittent) {
            current.Complete(now);
        } else {
            DateTime targetReachedAtUtc = current.StartedAtUtc.AddHours(current.TargetHours ?? 0);
            if (now >= targetReachedAtUtc) {
                current.Complete(now);
            } else {
                current.Interrupt(now);
            }
        }

        plan.Stop(now);
        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(current.ToModel(plan));
    }
}
