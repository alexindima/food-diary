using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicFastDay;

public sealed class SkipCyclicFastDayCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SkipCyclicFastDayCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        SkipCyclicFastDayCommand command, CancellationToken cancellationToken) {
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

        if (plan.Type != FastingPlanType.Cyclic || current.Kind != FastingOccurrenceKind.FastDay) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("Only an active cyclic fast day can be skipped."));
        }

        var now = dateTimeProvider.UtcNow;
        try {
            current.Skip(now);
            plan.ScheduleNextCyclicPhase(DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The current cyclic fast day cannot be skipped."));
        }

        var eatDayOccurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.EatDay,
            now,
            current.SequenceNumber + 1,
            targetHours: plan.CyclicEatDayEatingWindowHours,
            notes: current.Notes);

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken);
        await fastingOccurrenceRepository.AddAsync(eatDayOccurrence, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(eatDayOccurrence.ToModel(plan));
    }
}
