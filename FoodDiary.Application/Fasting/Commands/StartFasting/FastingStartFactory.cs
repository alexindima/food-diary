using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

internal static class FastingStartFactory {
    public static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> Create(
        StartFastingCommand command,
        UserId userId,
        DateTime startedAtUtc) {
        Result<FastingPlanType> planTypeResult = ResolvePlanType(command);
        if (planTypeResult.IsFailure) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(planTypeResult.Error);
        }

        try {
            return planTypeResult.Value switch {
                FastingPlanType.Intermittent => CreateIntermittent(command, userId, startedAtUtc),
                FastingPlanType.Extended => CreateExtended(command, userId, startedAtUtc),
                FastingPlanType.Cyclic => CreateCyclic(command, userId, startedAtUtc),
                _ => Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol),
            };
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol);
        }
    }

    private static Result<FastingPlanType> ResolvePlanType(StartFastingCommand command) {
        if (!string.IsNullOrWhiteSpace(command.PlanType)) {
            return EnumValueParser.ParseRequired<FastingPlanType>(command.PlanType, Errors.Fasting.InvalidProtocol);
        }

        FastingProtocol? protocol = EnumFilterParser.ParseOptional<FastingProtocol>(command.Protocol);
        if (protocol is null) {
            return Result.Success(FastingPlanType.Intermittent);
        }

        FastingPlanType planType = protocol.Value switch {
            FastingProtocol.F16_8 or FastingProtocol.F18_6 or FastingProtocol.F20_4 or FastingProtocol.CustomIntermittent => FastingPlanType.Intermittent,
            _ => FastingPlanType.Extended,
        };

        return Result.Success(planType);
    }

    private static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> CreateIntermittent(
        StartFastingCommand command,
        UserId userId,
        DateTime startedAtUtc) {
        Result<FastingProtocol> protocolResult = EnumValueParser.ParseRequired<FastingProtocol>(command.Protocol, Errors.Fasting.InvalidProtocol);
        if (protocolResult.IsFailure) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(protocolResult.Error);
        }

        FastingProtocol protocol = protocolResult.Value;
        int duration = command.PlannedDurationHours ?? FastingSession.GetDefaultDuration(protocol);
        if (protocol == FastingProtocol.CustomIntermittent && (duration < 1 || duration >= 24)) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol);
        }

        var plan = FastingPlan.CreateIntermittent(userId, protocol, duration, 24 - duration, startedAtUtc);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: duration,
            notes: command.Notes);

        return Result.Success((plan, occurrence));
    }

    private static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> CreateExtended(
        StartFastingCommand command,
        UserId userId,
        DateTime startedAtUtc) {
        Result<FastingProtocol> protocolResult = EnumValueParser.ParseRequired<FastingProtocol>(command.Protocol, Errors.Fasting.InvalidProtocol);
        if (protocolResult.IsFailure) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(protocolResult.Error);
        }

        FastingProtocol protocol = protocolResult.Value;
        int duration = command.PlannedDurationHours ?? FastingSession.GetDefaultDuration(protocol);
        var plan = FastingPlan.CreateExtended(userId, protocol, duration, startedAtUtc);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastDay,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: duration,
            notes: command.Notes);

        return Result.Success((plan, occurrence));
    }

    private static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> CreateCyclic(
        StartFastingCommand command,
        UserId userId,
        DateTime startedAtUtc) {
        int fastDays = command.CyclicFastDays ?? 1;
        int eatDays = command.CyclicEatDays ?? 1;
        int eatDayFastHours = command.CyclicEatDayFastHours ?? 16;
        int eatDayEatingWindowHours = command.CyclicEatDayEatingWindowHours ?? 8;

        var plan = FastingPlan.CreateCyclic(
            userId,
            fastDays,
            eatDays,
            eatDayFastHours,
            eatDayEatingWindowHours,
            startedAtUtc,
            startedAtUtc);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastDay,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: 24,
            notes: command.Notes);

        return Result.Success((plan, occurrence));
    }
}
