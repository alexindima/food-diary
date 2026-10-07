import type { DashboardSnapshot } from '../../models/dashboard.data';
import type { TdeeInsight } from '../../models/tdee-insight.data';
import { cycleFromSdk } from './cycle-sdk.mapper';
import { fastingSessionFromSdk } from './fasting-sdk.mapper';
import type { DashboardSnapshotHttpResponse } from './generated/model/dashboard-snapshot-http-response';
import type { TdeeInsightHttpResponse } from './generated/model/tdee-insight-http-response';
import { mealFromSdk } from './meal-sdk.mapper';
import { waistSummaryFromSdk, weightSummaryFromSdk } from './measurement-sdk.mapper';
import { requireSdkFields, sdkEnum, sdkMaybe, sdkNullableFields, sdkOptional } from './sdk-response';

export function tdeeInsightFromSdk(response: TdeeInsightHttpResponse): TdeeInsight {
    const value = requireSdkFields(response, ['confidence', 'dataDaysUsed']);
    return {
        ...sdkNullableFields(value, [
            'estimatedTdee',
            'adaptiveTdee',
            'bmr',
            'suggestedCalorieTarget',
            'currentCalorieTarget',
            'weightTrendPerWeek',
            'goalAdjustmentHint',
        ]),
        confidence: sdkEnum(value.confidence, ['none', 'low', 'medium', 'high'] as const),
    };
}

export function dashboardSnapshotFromSdk(response: DashboardSnapshotHttpResponse): DashboardSnapshot {
    const value = requireSdkFields(response, [
        'date',
        'dateTo',
        'dailyGoal',
        'weeklyCalorieGoal',
        'statistics',
        'weeklyCalories',
        'weight',
        'waist',
        'meals',
    ]);
    const meals = requireSdkFields(value.meals, ['items', 'total']);
    return {
        ...value,
        statistics: requireSdkFields(value.statistics, ['totalCalories', 'averageProteins', 'averageFats', 'averageCarbs', 'averageFiber']),
        weeklyCalories: value.weeklyCalories.map(point => requireSdkFields(point, ['date', 'calories'])),
        weight: {
            ...value.weight,
            latest: sdkOptional(value.weight.latest, point => requireSdkFields(point, ['date', 'weightKg'])),
            previous: sdkOptional(value.weight.previous, point => requireSdkFields(point, ['date', 'weightKg'])),
            desiredWeightKg: value.weight.desiredWeightKg ?? null,
        },
        waist: {
            ...value.waist,
            latest: sdkOptional(value.waist.latest, point => requireSdkFields(point, ['date', 'circumferenceCm'])),
            previous: sdkOptional(value.waist.previous, point => requireSdkFields(point, ['date', 'circumferenceCm'])),
            desiredWaistCm: value.waist.desiredWaistCm ?? null,
        },
        meals: { ...meals, items: meals.items.map(mealFromSdk) },
        weightTrend: value.weightTrend?.map(weightSummaryFromSdk),
        waistTrend: value.waistTrend?.map(waistSummaryFromSdk),
        hydration: sdkMaybe(value.hydration, hydration =>
            sdkNullableFields(requireSdkFields(hydration, ['dateUtc', 'totalMl']), ['goalMl']),
        ),
        advice: sdkMaybe(value.advice, advice => requireSdkFields(advice, ['id', 'locale', 'value', 'weight'])),
        currentFastingSession: sdkMaybe(value.currentFastingSession, fastingSessionFromSdk),
        currentCycle: sdkMaybe(value.currentCycle, cycleFromSdk),
        tdeeInsight: sdkMaybe(value.tdeeInsight, tdeeInsightFromSdk),
        dashboardLayout: sdkMaybe(value.dashboardLayout, layout => ({
            ...layout,
            web: layout.web ?? undefined,
            mobile: layout.mobile ?? undefined,
        })),
    };
}
