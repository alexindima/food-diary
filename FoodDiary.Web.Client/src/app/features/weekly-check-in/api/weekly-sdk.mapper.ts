import type { WeekSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/week-summary-http-response';
import type { WeeklyCheckInHttpResponse } from '../../../shared/api/sdk/generated/model/weekly-check-in-http-response';
import type { WeeklyGoalHttpResponse } from '../../../shared/api/sdk/generated/model/weekly-goal-http-response';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import type { WeeklyCheckInData, WeekSummary } from '../models/weekly-check-in.data';
import type { WeeklyGoal } from '../models/weekly-goal.data';

export function weeklyGoalFromSdk(response: WeeklyGoalHttpResponse): WeeklyGoal {
    const value = requireSdkFields(response, ['id', 'weekStart', 'type', 'targetDays', 'progressDays', 'isCompleted', 'reminderEnabled']);
    return {
        ...value,
        type: sdkEnum(value.type, ['DiaryLogging'] as const),
        reminderTime: value.reminderTime ?? null,
        timeZoneOffsetMinutes: value.timeZoneOffsetMinutes ?? null,
    };
}

function weekSummaryFromSdk(response: WeekSummaryHttpResponse): WeekSummary {
    const value = requireSdkFields(response, [
        'totalCalories',
        'avgDailyCalories',
        'avgProteins',
        'avgFats',
        'avgCarbs',
        'mealsLogged',
        'daysLogged',
        'totalHydrationMl',
        'avgDailyHydrationMl',
    ]);
    return {
        ...value,
        weightStart: value.weightStart ?? null,
        weightEnd: value.weightEnd ?? null,
        waistStart: value.waistStart ?? null,
        waistEnd: value.waistEnd ?? null,
    };
}

export function weeklyCheckInFromSdk(response: WeeklyCheckInHttpResponse): WeeklyCheckInData {
    const value = requireSdkFields(response, ['thisWeek', 'lastWeek', 'trends', 'suggestions']);
    const trends = requireSdkFields(value.trends, [
        'calorieChange',
        'proteinChange',
        'fatChange',
        'carbChange',
        'hydrationChange',
        'mealsLoggedChange',
    ]);
    return {
        thisWeek: weekSummaryFromSdk(value.thisWeek),
        lastWeek: weekSummaryFromSdk(value.lastWeek),
        suggestions: value.suggestions,
        trends: { ...trends, weightChange: trends.weightChange ?? null, waistChange: trends.waistChange ?? null },
    };
}
