using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Application.Common;
using FoodDiary.Modules.Fasting.Application.Mappings;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Fasting.Application.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Application.Commands.EndFasting;

public sealed class EndFastingCommandHandler(
    IFastingPlanWriteRepository fastingPlanRepository,
    IFastingOccurrenceWriteRepository fastingOccurrenceRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : ICommandHandler<EndFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        EndFastingCommand command, CancellationToken cancellationToken) {
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

        FastingPlan? plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        FastingEndPolicy.End(plan, current, now);
        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        return Result.Success(current.ToModel(plan));
    }
}
