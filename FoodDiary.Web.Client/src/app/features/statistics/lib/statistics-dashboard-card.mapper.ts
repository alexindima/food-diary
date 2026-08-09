import type { User } from '../../../shared/models/user.data';
import type { MappedStatistics } from '../models/statistics.data';
import type {
    StatisticsBodyTrendData,
    StatisticsDietStabilityData,
    StatisticsMealStructureData,
    StatisticsNutrientBalanceItem,
    StatisticsNutritionDay,
    StatisticsOverviewData,
    StatisticsTrendInsight,
} from '../models/statistics-dashboard-card.models';

const CALORIE_GOAL_TOLERANCE = 0.1;
const PERCENT = 100;
const STABILITY_DEVIATION_TOLERANCE_PERCENT = 20;

export type StatisticsDashboardCardsView = {
    overview: StatisticsOverviewData;
    days: readonly StatisticsNutritionDay[];
    insights: readonly StatisticsTrendInsight[];
    balance: readonly StatisticsNutrientBalanceItem[];
    mealStructure: StatisticsMealStructureData;
    stability: StatisticsDietStabilityData;
    body: StatisticsBodyTrendData;
};

export type StatisticsDashboardCardsInput = {
    statistics: MappedStatistics | null;
    user: User | null;
    weightPoints: ReadonlyArray<{ label: string; value: number | null }>;
    waistPoints: ReadonlyArray<{ label: string; value: number | null }>;
    quantizationDays: number;
    periodDays: number;
    formatDate: (date: Date) => string;
};

export function buildStatisticsDashboardCardsView(input: StatisticsDashboardCardsInput): StatisticsDashboardCardsView {
    const { statistics, user, weightPoints, waistPoints, quantizationDays, periodDays, formatDate } = input;
    const calorieGoal = user?.dailyCalorieTarget ?? 0;
    const trackedIndexes = getTrackedIndexes(statistics);
    const averageCalories = average(trackedIndexes.map(index => statistics?.calories[index] ?? 0));
    const nutrients = buildNutrients(statistics, user, trackedIndexes);
    const calorieDifferencePercent = getDifferencePercent(averageCalories, calorieGoal);

    const days = buildDays(statistics, formatDate);

    return {
        overview: {
            daysWithinGoal: countDaysWithinGoal(statistics, trackedIndexes, calorieGoal),
            trackedDays: trackedIndexes.length,
            periodDays,
            averageCalories,
            calorieGoal,
            calorieChangePercent: calorieDifferencePercent,
            nutrients,
        },
        days,
        insights: buildInsights(nutrients, calorieDifferencePercent),
        balance: nutrients,
        mealStructure: buildMealStructure(statistics),
        stability: buildDietStability(days, calorieGoal, quantizationDays),
        body: {
            weight: buildBodyMetric('weight', weightPoints, user?.desiredWeight ?? null, periodDays),
            waist: buildBodyMetric('waist', waistPoints, user?.desiredWaist ?? null, periodDays),
        },
    };
}

function buildMealStructure(statistics: MappedStatistics | null): StatisticsMealStructureData {
    const totals = statistics?.mealStructure;
    if (totals === undefined) {
        return buildEmptyMealStructure();
    }
    const source: ReadonlyArray<{ key: StatisticsMealStructureData['items'][number]['key']; calories: number }> = [
        { key: 'breakfast', calories: totals.breakfastCalories },
        { key: 'lunch', calories: totals.lunchCalories },
        { key: 'dinner', calories: totals.dinnerCalories },
        { key: 'snack', calories: totals.snackCalories },
    ];
    const periodCalories = source.reduce((sum, item) => sum + item.calories, 0);
    const trackedDayCount = totals.trackedDayCount;
    const percentages = allocatePercentages(source.map(item => item.calories));
    const items = source.map((item, index) => buildMealStructureItem(item, trackedDayCount, percentages[index] ?? 0));
    const dominant = getDominantMeal(items);

    return {
        totalCalories: trackedDayCount <= 0 ? 0 : periodCalories / trackedDayCount,
        averageMealsPerDay: trackedDayCount <= 0 ? 0 : totals.mealCount / trackedDayCount,
        dominantMeal: dominant !== null && dominant.calories > 0 ? dominant.key : null,
        items,
    };
}

function buildEmptyMealStructure(): StatisticsMealStructureData {
    return {
        totalCalories: 0,
        averageMealsPerDay: 0,
        dominantMeal: null,
        items: [
            { key: 'breakfast', calories: 0, percentage: 0 },
            { key: 'lunch', calories: 0, percentage: 0 },
            { key: 'dinner', calories: 0, percentage: 0 },
            { key: 'snack', calories: 0, percentage: 0 },
        ],
    };
}

function buildMealStructureItem(
    item: { key: StatisticsMealStructureData['items'][number]['key']; calories: number },
    trackedDayCount: number,
    percentage: number,
): StatisticsMealStructureData['items'][number] {
    return {
        ...item,
        calories: trackedDayCount <= 0 ? 0 : item.calories / trackedDayCount,
        percentage,
    };
}

function allocatePercentages(values: readonly number[]): number[] {
    const total = values.reduce((sum, value) => sum + value, 0);
    if (total <= 0) {
        return values.map(() => 0);
    }

    const exact = values.map(value => (value / total) * PERCENT);
    const result = exact.map(Math.floor);
    const remainder = PERCENT - result.reduce((sum, value) => sum + value, 0);
    const indexesByRemainder = exact
        .map((value, index) => ({ index, remainder: value - Math.floor(value) }))
        .sort((left, right) => {
            const difference = right.remainder - left.remainder;
            return difference !== 0 ? difference : left.index - right.index;
        });

    for (let index = 0; index < remainder; index += 1) {
        const target = indexesByRemainder[index].index;
        result[target] = result[target] + 1;
    }

    return result;
}

function getDominantMeal(items: StatisticsMealStructureData['items']): StatisticsMealStructureData['items'][number] | null {
    return items.reduce<StatisticsMealStructureData['items'][number] | null>(
        (current, item) => (item.calories > (current?.calories ?? 0) ? item : current),
        null,
    );
}

function buildDietStability(
    days: readonly StatisticsNutritionDay[],
    dailyCalorieGoal: number,
    quantizationDays: number,
): StatisticsDietStabilityData {
    const hasGoal = dailyCalorieGoal > 0;
    const intervalGoal = dailyCalorieGoal * quantizationDays;
    const trackedDays = days.filter(day => day.calories !== null);
    const deviations = hasGoal ? trackedDays.map(day => Math.abs(((day.calories ?? 0) - intervalGoal) / intervalGoal) * PERCENT) : [];
    const statuses = days.map(day => ({
        label: day.label,
        status:
            !hasGoal || day.calories === null
                ? ('missing' as const)
                : (deviations[trackedDays.indexOf(day)] ?? PERCENT) <= STABILITY_DEVIATION_TOLERANCE_PERCENT
                  ? ('stable' as const)
                  : ('deviation' as const),
    }));

    return {
        stableCount: statuses.filter(day => day.status === 'stable').length,
        totalCount: days.length,
        averageDeviationPercent: deviations.length === 0 ? null : Math.round(average(deviations)),
        longestLoggingStreak: getLongestLoggingStreak(days),
        usesDailyIntervals: quantizationDays === 1,
        hasGoal,
        days: statuses,
    };
}

function getLongestLoggingStreak(days: readonly StatisticsNutritionDay[]): number {
    let longest = 0;
    let current = 0;
    for (const day of days) {
        current = day.calories === null ? 0 : current + 1;
        longest = Math.max(longest, current);
    }
    return longest;
}

function getTrackedIndexes(statistics: MappedStatistics | null): number[] {
    return (
        statistics?.calories.reduce<number[]>((indexes, calories, index) => {
            if (calories > 0) {
                indexes.push(index);
            }
            return indexes;
        }, []) ?? []
    );
}

// eslint-disable-next-line complexity -- Each optional profile target and statistics series has an explicit empty-data fallback.
function buildNutrients(
    statistics: MappedStatistics | null,
    user: User | null,
    trackedIndexes: readonly number[],
): StatisticsNutrientBalanceItem[] {
    const source = [
        { key: 'protein' as const, values: statistics?.nutrientsStatistic.proteins ?? [], goal: user?.proteinTarget ?? 0 },
        { key: 'fat' as const, values: statistics?.nutrientsStatistic.fats ?? [], goal: user?.fatTarget ?? 0 },
        { key: 'carbs' as const, values: statistics?.nutrientsStatistic.carbs ?? [], goal: user?.carbTarget ?? 0 },
        { key: 'fiber' as const, values: statistics?.nutrientsStatistic.fiber ?? [], goal: user?.fiberTarget ?? 0 },
    ];
    return source.map(item => ({
        key: item.key,
        current: average(trackedIndexes.map(index => item.values[index] ?? 0)),
        goal: item.goal,
    }));
}

function countDaysWithinGoal(statistics: MappedStatistics | null, indexes: readonly number[], goal: number): number {
    if (goal <= 0) {
        return 0;
    }
    return indexes.filter(index => Math.abs((statistics?.calories[index] ?? 0) - goal) <= goal * CALORIE_GOAL_TOLERANCE).length;
}

function buildDays(statistics: MappedStatistics | null, formatDate: (date: Date) => string): StatisticsNutritionDay[] {
    // eslint-disable-next-line complexity -- Every optional nutrient series is independently normalized for a chart day.
    return (statistics?.date ?? []).map((date, index) => {
        const calories = statistics?.calories[index] ?? 0;
        return {
            date: date.toISOString(),
            label: formatDate(date),
            calories: calories > 0 ? calories : null,
            protein: statistics?.nutrientsStatistic.proteins[index] ?? 0,
            fat: statistics?.nutrientsStatistic.fats[index] ?? 0,
            carbs: statistics?.nutrientsStatistic.carbs[index] ?? 0,
            fiber: statistics?.nutrientsStatistic.fiber[index] ?? 0,
        };
    });
}

function buildInsights(items: readonly StatisticsNutrientBalanceItem[], calories: number | null): StatisticsTrendInsight[] {
    const protein = items.find(item => item.key === 'protein');
    const proteinDifference = protein !== undefined && protein.goal > 0 ? protein.current - protein.goal : null;
    return [
        createInsight({
            key: 'calories',
            labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES',
            value: calories,
            unitKey: 'STATISTICS.DASHBOARD.TREND.PERCENT_UNIT',
            positive: isClose(calories),
        }),
        createInsight({
            key: 'protein',
            labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.PROTEIN',
            value: proteinDifference,
            unitKey: 'STATISTICS.DASHBOARD.TREND.GRAM_UNIT',
            positive: proteinDifference !== null && proteinDifference >= 0,
        }),
        {
            key: 'completeness',
            labelKey: 'STATISTICS.DASHBOARD.TREND.INCOMPLETE_DATA',
            value: null,
            unitKey: '',
            tone: 'neutral',
            detailKey: 'STATISTICS.DASHBOARD.TREND.INCOMPLETE_DATA_HINT',
        },
    ];
}

function createInsight(input: Omit<StatisticsTrendInsight, 'tone' | 'detailKey'> & { positive: boolean }): StatisticsTrendInsight {
    const { key, labelKey, value, unitKey, positive } = input;
    return { key, labelKey, value, unitKey, tone: positive ? 'positive' : 'neutral', detailKey: 'STATISTICS.DASHBOARD.TREND.VS_GOAL' };
}

function buildBodyMetric(
    key: 'weight' | 'waist',
    points: ReadonlyArray<{ label: string; value: number | null }>,
    goal: number | null,
    days: number,
): StatisticsBodyTrendData['weight'] {
    const values = points.filter((point): point is { label: string; value: number } => point.value !== null);
    const current = values.at(-1)?.value ?? null;
    const first = values[0]?.value ?? null;
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- Empty body series intentionally exposes null metrics.
    return { key, current, change: current === null || first === null ? null : current - first, goal, timeframeDays: days, points };
}

function getDifferencePercent(current: number, goal: number): number | null {
    return goal <= 0 ? null : Math.round(((current - goal) / goal) * PERCENT);
}

function isClose(value: number | null): boolean {
    return value !== null && Math.abs(value) <= PERCENT * CALORIE_GOAL_TOLERANCE;
}

function average(values: readonly number[]): number {
    return values.length === 0 ? 0 : values.reduce((sum, value) => sum + value, 0) / values.length;
}
