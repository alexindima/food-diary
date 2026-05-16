import type { NutrientBar } from '../../../../components/shared/dashboard-summary-card/dashboard-summary-card.types';
import type { MealPreviewEntry } from '../../../../components/shared/meals-preview/meals-preview.types';
import type { CyclePredictions } from '../../../cycle-tracking/models/cycle.data';
import type { FastingSession } from '../../../fasting/models/fasting.data';
import type { WeightTrendPoint } from '../../components/weight-trend-card/weight-trend-card.component';
import type { DailyAdvice } from '../../models/daily-advice.data';
import type { TdeeInsight } from '../../models/tdee-insight.data';

export type DashboardHeaderState = {
    fullTitleKey: string;
    compactTitleKey: string;
    titleParams: { date: string } | null;
    selectedDateLabel: string;
};

export type DashboardBlockId = 'fasting' | 'summary' | 'meals' | 'hydration' | 'cycle' | 'weight' | 'waist' | 'tdee' | 'advice';

export type DashboardBlockState = {
    hidden: boolean;
    role: 'button' | null;
    tabIndex: number;
    ariaPressed: boolean | null;
    ariaDisabled: boolean | null;
    ariaLabel: string | null;
    inert: string | null;
};

export type DashboardBlockStateOptions = {
    alwaysInteractive?: boolean;
    locked?: boolean;
    editingLabelKey?: string;
    defaultLabelKey?: string;
};

export type DashboardMealsPreviewState = {
    titleText: string | null;
    emptyKey: string;
    showDateActions: boolean;
    showEmptyState: boolean;
};

export type DashboardHydrationCardState = {
    total: number;
    goal: number | null;
};

export type DashboardCycleCardState = {
    startDate: string | null;
    predictions: CyclePredictions | null;
};

export type DashboardFastingSession = FastingSession | null;

export type DashboardSummaryData = {
    dailyGoal: number;
    dailyConsumed: number;
    weeklyConsumed: number;
    weeklyGoal: number | null;
    nutrientBars: NutrientBar[] | null;
};

export type DashboardMealPreviewEntry = MealPreviewEntry;
export type DashboardWeightTrendPoint = WeightTrendPoint;
export type DashboardDailyAdvice = DailyAdvice | null;
export type DashboardTdeeInsight = TdeeInsight | null;
