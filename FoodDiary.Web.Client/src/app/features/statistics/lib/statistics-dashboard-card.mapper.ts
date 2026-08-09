import type { User } from '../../../shared/models/user.data';
import type { MappedStatistics } from '../models/statistics.data';
import type {
    StatisticsBodyTrendData,
    StatisticsNutrientBalanceItem,
    StatisticsNutritionDay,
    StatisticsOverviewData,
    StatisticsTrendInsight,
} from '../models/statistics-dashboard-card.models';

const CALORIE_GOAL_TOLERANCE = 0.1;
const PERCENT = 100;

export type StatisticsDashboardCardsView = {
    overview: StatisticsOverviewData;
    days: readonly StatisticsNutritionDay[];
    insights: readonly StatisticsTrendInsight[];
    balance: readonly StatisticsNutrientBalanceItem[];
    body: StatisticsBodyTrendData;
};

export type StatisticsDashboardCardsInput = {
    statistics: MappedStatistics | null;
    user: User | null;
    bodyPoints: ReadonlyArray<{ label: string; value: number | null }>;
    periodDays: number;
    formatDate: (date: Date) => string;
};

export function buildStatisticsDashboardCardsView(input: StatisticsDashboardCardsInput): StatisticsDashboardCardsView {
    const { statistics, user, bodyPoints, periodDays, formatDate } = input;
    const calorieGoal = user?.dailyCalorieTarget ?? 0;
    const trackedIndexes = getTrackedIndexes(statistics);
    const averageCalories = average(trackedIndexes.map(index => statistics?.calories[index] ?? 0));
    const nutrients = buildNutrients(statistics, user, trackedIndexes);
    const calorieDifferencePercent = getDifferencePercent(averageCalories, calorieGoal);

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
        days: buildDays(statistics, formatDate),
        insights: buildInsights(nutrients, calorieDifferencePercent),
        balance: nutrients,
        body: buildBody(bodyPoints, periodDays),
    };
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

function buildBody(points: ReadonlyArray<{ label: string; value: number | null }>, days: number): StatisticsBodyTrendData {
    const values = points.filter((point): point is { label: string; value: number } => point.value !== null);
    const current = values.at(-1)?.value ?? null;
    const first = values[0]?.value ?? null;
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- Empty body series intentionally exposes null metrics.
    return { currentWeight: current, change: current === null || first === null ? null : current - first, timeframeDays: days, points };
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
