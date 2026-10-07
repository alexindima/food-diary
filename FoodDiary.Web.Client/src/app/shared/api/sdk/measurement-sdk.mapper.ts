import type { WaistEntry, WaistEntrySummaryPoint, WaistHistoryPageSummary } from '../../models/waist-entry.data';
import type { WeightEntry, WeightEntrySummaryPoint, WeightHistoryPageSummary } from '../../models/weight-entry.data';
import type { WaistEntryHttpResponse } from './generated/model/waist-entry-http-response';
import type { WaistEntrySummaryHttpResponse } from './generated/model/waist-entry-summary-http-response';
import type { WaistHistoryPageSummaryHttpResponse } from './generated/model/waist-history-page-summary-http-response';
import type { WeightEntryHttpResponse } from './generated/model/weight-entry-http-response';
import type { WeightEntrySummaryHttpResponse } from './generated/model/weight-entry-summary-http-response';
import type { WeightHistoryPageSummaryHttpResponse } from './generated/model/weight-history-page-summary-http-response';
import { requireSdkFields, sdkEnum } from './sdk-response';

const GOAL_STATUSES = ['Active', 'Replaced', 'Cancelled'] as const;

export function weightEntryFromSdk(value: WeightEntryHttpResponse): WeightEntry {
    return requireSdkFields(value, ['id', 'userId', 'date', 'weightKg']);
}

export function waistEntryFromSdk(value: WaistEntryHttpResponse): WaistEntry {
    return requireSdkFields(value, ['id', 'userId', 'date', 'circumferenceCm']);
}

export function weightSummaryFromSdk(value: WeightEntrySummaryHttpResponse): WeightEntrySummaryPoint {
    return requireSdkFields(value, ['startDate', 'endDate', 'averageWeightKg']);
}

export function waistSummaryFromSdk(value: WaistEntrySummaryHttpResponse): WaistEntrySummaryPoint {
    return requireSdkFields(value, ['startDate', 'endDate', 'averageCircumferenceCm']);
}

export function weightPageSummaryFromSdk(response: WeightHistoryPageSummaryHttpResponse): WeightHistoryPageSummary {
    const value = requireSdkFields(response, ['entries', 'summary', 'goal', 'goalHistory']);
    return {
        entries: value.entries.map(weightEntryFromSdk),
        summary: value.summary.map(weightSummaryFromSdk),
        heightCm: value.heightCm ?? null,
        goal: {
            desiredWeightKg: value.goal.desiredWeightKg ?? null,
            startWeightKg: value.goal.startWeightKg ?? null,
            startedAtUtc: value.goal.startedAtUtc ?? null,
        },
        goalHistory: value.goalHistory.map(row => {
            const item = requireSdkFields(row, ['id', 'targetWeightKg', 'startWeightKg', 'startedAtUtc', 'status']);
            return {
                ...item,
                endWeightKg: item.endWeightKg ?? null,
                endedAtUtc: item.endedAtUtc ?? null,
                status: sdkEnum(item.status, GOAL_STATUSES),
            };
        }),
    };
}

export function waistPageSummaryFromSdk(response: WaistHistoryPageSummaryHttpResponse): WaistHistoryPageSummary {
    const value = requireSdkFields(response, ['entries', 'summary', 'goal', 'goalHistory']);
    return {
        entries: value.entries.map(waistEntryFromSdk),
        summary: value.summary.map(waistSummaryFromSdk),
        heightCm: value.heightCm ?? null,
        goal: {
            desiredWaistCm: value.goal.desiredWaistCm ?? null,
            startWaistCm: value.goal.startWaistCm ?? null,
            startedAtUtc: value.goal.startedAtUtc ?? null,
        },
        goalHistory: value.goalHistory.map(row => {
            const item = requireSdkFields(row, ['id', 'targetWaistCm', 'startWaistCm', 'startedAtUtc', 'status']);
            return {
                ...item,
                endWaistCm: item.endWaistCm ?? null,
                endedAtUtc: item.endedAtUtc ?? null,
                status: sdkEnum(item.status, GOAL_STATUSES),
            };
        }),
    };
}
