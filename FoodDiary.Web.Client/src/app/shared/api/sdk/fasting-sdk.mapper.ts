import type { FastingMessage, FastingOverview, FastingSession } from '../../models/fasting.data';
import type { FastingMessageHttpResponse } from './generated/model/fasting-message-http-response';
import type { FastingOverviewHttpResponse } from './generated/model/fasting-overview-http-response';
import type { FastingSessionHttpResponse } from './generated/model/fasting-session-http-response';
import { requireSdkFields, sdkEnum, sdkNullableFields, sdkOptional, sdkPage } from './sdk-response';

export function fastingSessionFromSdk(response: FastingSessionHttpResponse): FastingSession {
    const value = requireSdkFields(response, [
        'id',
        'startedAtUtc',
        'initialPlannedDurationHours',
        'addedDurationHours',
        'plannedDurationHours',
        'protocol',
        'planType',
        'occurrenceKind',
        'isCompleted',
        'status',
        'symptoms',
        'checkIns',
    ]);
    const nullable = sdkNullableFields(value, [
        'endedAtUtc',
        'cyclicFastDays',
        'cyclicEatDays',
        'cyclicEatDayFastHours',
        'cyclicEatDayEatingWindowHours',
        'cyclicPhaseDayNumber',
        'cyclicPhaseDayTotal',
        'notes',
        'checkInAtUtc',
        'hungerLevel',
        'energyLevel',
        'moodLevel',
        'checkInNotes',
    ]);
    return {
        ...nullable,
        planType: sdkEnum(value.planType, ['Intermittent', 'Extended', 'Cyclic'] as const),
        occurrenceKind: sdkEnum(value.occurrenceKind, ['FastingWindow', 'EatingWindow', 'FastDay', 'EatDay'] as const),
        status: sdkEnum(value.status, ['Active', 'Completed', 'Interrupted', 'Skipped', 'Postponed'] as const),
        checkIns: value.checkIns.map(checkInResponse => {
            const item = requireSdkFields(checkInResponse, ['id', 'checkedInAtUtc', 'hungerLevel', 'energyLevel', 'moodLevel', 'symptoms']);
            return { ...item, notes: item.notes ?? null };
        }),
    };
}

function fastingMessageFromSdk(response: FastingMessageHttpResponse): FastingMessage {
    const value = requireSdkFields(response, ['id', 'titleKey', 'bodyKey', 'tone']);
    return { ...value, tone: sdkEnum(value.tone, ['warning', 'positive', 'neutral'] as const), bodyParams: value.bodyParams ?? null };
}

export function fastingOverviewFromSdk(response: FastingOverviewHttpResponse): FastingOverview {
    const value = requireSdkFields(response, ['stats', 'insights', 'history']);
    const stats = requireSdkFields(value.stats, [
        'totalCompleted',
        'currentStreak',
        'averageDurationHours',
        'completionRateLast30Days',
        'checkInRateLast30Days',
    ]);
    const insights = requireSdkFields(value.insights, ['alerts', 'insights']);
    return {
        currentSession: sdkOptional(value.currentSession, fastingSessionFromSdk),
        stats: { ...stats, lastCheckInAtUtc: stats.lastCheckInAtUtc ?? null, topSymptom: stats.topSymptom ?? null },
        insights: { alerts: insights.alerts.map(fastingMessageFromSdk), insights: insights.insights.map(fastingMessageFromSdk) },
        history: sdkPage(value.history, fastingSessionFromSdk),
    };
}
