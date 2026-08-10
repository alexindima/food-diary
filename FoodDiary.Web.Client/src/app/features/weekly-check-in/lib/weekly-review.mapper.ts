import type { WeekSummary, WeekTrend } from '../models/weekly-check-in.data';
import type { WeeklyReviewInsightViewModel, WeeklyReviewViewModel } from './weekly-check-in.types';

const DAYS_IN_WEEK = 7;
const MINIMUM_RELIABLE_DAYS = 4;
const LOGGING_FOCUS_DAYS = 5;

export function buildWeeklyReview(
    week: WeekSummary | undefined,
    trends: WeekTrend | undefined,
    suggestions: string[],
): WeeklyReviewViewModel | null {
    if (week === undefined) {
        return null;
    }

    const daysLogged = Math.min(Math.max(week.daysLogged, 0), DAYS_IN_WEEK);
    const hasEnoughData = daysLogged >= MINIMUM_RELIABLE_DAYS;

    return {
        daysLogged,
        hasEnoughData,
        summaryKey: hasEnoughData ? 'WEEKLY_CHECK_IN.SUMMARY.RELIABLE' : 'WEEKLY_CHECK_IN.SUMMARY.LIMITED',
        focusTitleKey: daysLogged < LOGGING_FOCUS_DAYS ? 'WEEKLY_CHECK_IN.FOCUS.LOGGING_TITLE' : 'WEEKLY_CHECK_IN.FOCUS.CONSISTENCY_TITLE',
        focusDescriptionKey:
            daysLogged < LOGGING_FOCUS_DAYS ? 'WEEKLY_CHECK_IN.FOCUS.LOGGING_DESCRIPTION' : 'WEEKLY_CHECK_IN.FOCUS.CONSISTENCY_DESCRIPTION',
        focusTarget: daysLogged < LOGGING_FOCUS_DAYS ? LOGGING_FOCUS_DAYS : DAYS_IN_WEEK,
        insights: buildInsights(trends, suggestions, daysLogged),
    };
}

function buildInsights(trends: WeekTrend | undefined, suggestions: string[], daysLogged: number): WeeklyReviewInsightViewModel[] {
    const insights: WeeklyReviewInsightViewModel[] = [];

    if (trends !== undefined && trends.hydrationChange > 0) {
        insights.push(createInsight('hydration', 'water_drop', 'positive', 'WEEKLY_CHECK_IN.INSIGHTS.HYDRATION_IMPROVED'));
    }

    if (suggestions.includes('suggestion.add_protein') || suggestions.includes('ADD_PROTEIN')) {
        insights.push(createInsight('protein', 'fitness_center', 'attention', 'WEEKLY_CHECK_IN.INSIGHTS.PROTEIN_LOW'));
    }

    if (daysLogged > 0) {
        insights.push(createInsight('logging', 'event_available', 'info', getLoggingProgressLabelKey(daysLogged)));
    }

    if (insights.length === 0) {
        insights.push(createInsight('start', 'auto_awesome', 'info', 'WEEKLY_CHECK_IN.INSIGHTS.START_LOGGING'));
    }

    return insights;
}

function getLoggingProgressLabelKey(daysLogged: number): string {
    if (daysLogged === 1) {
        return 'WEEKLY_CHECK_IN.INSIGHTS.LOGGING_PROGRESS_ONE';
    }

    if (daysLogged < LOGGING_FOCUS_DAYS) {
        return 'WEEKLY_CHECK_IN.INSIGHTS.LOGGING_PROGRESS_FEW';
    }

    return 'WEEKLY_CHECK_IN.INSIGHTS.LOGGING_PROGRESS_MANY';
}

function createInsight(
    key: string,
    icon: string,
    tone: WeeklyReviewInsightViewModel['tone'],
    labelKey: string,
): WeeklyReviewInsightViewModel {
    return { key, icon, tone, labelKey };
}
