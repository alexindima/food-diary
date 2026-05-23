import type { ClientSummary, DietologistPermissions } from '../../../../../shared/models/dietologist.data';
import type { DietologistClientGoals, DietologistRecommendation } from '../../../../../shared/models/dietologist.data';
import type { DashboardSnapshot } from '../../../../dashboard/models/dashboard.data';
import type { FastingSession } from '../../../../fasting/models/fasting.data';
import type { Meal } from '../../../../meals/models/meal.data';

const PERCENT_MAX = 100;
const DATE_ONLY_LENGTH = 10;
const MEAL_ITEM_PREVIEW_LIMIT = 3;
const STABLE_DELTA_THRESHOLD = 0.05;
const ONE_DECIMAL_PRECISION = 10;

export type ClientDashboardSection = {
    isVisible: boolean;
    titleKey: string;
    bodyKey: string;
};

export type ClientMetricTile = {
    labelKey: string;
    value: string;
};

export type ClientProfileDetail = {
    labelKey: string;
    value: string;
};

export type ClientMealView = {
    id: string;
    title: string;
    date: string;
    calories: string;
    macros: string;
    itemSummary: string;
};

export type ClientBodyMeasurementView = {
    date: string;
    value: string;
    delta: string | null;
};

export type ClientHydrationView = {
    total: string;
    goal: string | null;
    progress: number | null;
};

export type ClientFastingView = {
    status: string;
    protocol: string;
    startedAtUtc: string;
    plannedDuration: string;
    checkInSummary: string | null;
};

export type ClientRecommendationView = {
    id: string;
    text: string;
    createdAtUtc: string;
    statusKey: string;
};

export function getClientDashboardTitle(client: ClientSummary): string {
    const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
    return fullName.length > 0 ? fullName : client.email;
}

export function buildClientProfileChips(client: ClientSummary | null): string[] {
    if (client?.permissions.shareProfile !== true) {
        return [];
    }

    return [formatProfileChip(client.height, ' cm'), client.gender, client.activityLevel].filter(
        (value): value is string => value !== null && value.length > 0,
    );
}

export function buildClientProfileDetails(client: ClientSummary | null): ClientProfileDetail[] {
    if (client?.permissions.shareProfile !== true) {
        return [];
    }

    return [
        { labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.PROFILE.EMAIL', value: client.email },
        { labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.PROFILE.HEIGHT', value: formatNullableNumber(client.height, ' cm') },
        { labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.PROFILE.GENDER', value: client.gender ?? '-' },
        { labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.PROFILE.ACTIVITY', value: client.activityLevel ?? '-' },
        { labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.PROFILE.BIRTH_DATE', value: formatDateOnly(client.birthDate) },
    ];
}

export function buildClientDashboardSections(client: ClientSummary | null): ClientDashboardSection[] {
    const permissions = client?.permissions;
    if (permissions === undefined) {
        return [];
    }

    return [
        {
            isVisible: permissions.shareProfile,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.PROFILE_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.PROFILE_BODY',
        },
        {
            isVisible: permissions.shareStatistics,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.STATISTICS_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.STATISTICS_BODY',
        },
        {
            isVisible: permissions.shareMeals,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.MEALS_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.MEALS_BODY',
        },
        {
            isVisible: permissions.shareWeight,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WEIGHT_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WEIGHT_BODY',
        },
        {
            isVisible: permissions.shareWaist,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WAIST_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WAIST_BODY',
        },
        {
            isVisible: permissions.shareGoals,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.GOALS_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.GOALS_BODY',
        },
        {
            isVisible: permissions.shareHydration,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.HYDRATION_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.HYDRATION_BODY',
        },
        {
            isVisible: permissions.shareFasting,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.FASTING_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.FASTING_BODY',
        },
    ].filter(section => section.isVisible);
}

export function buildNutritionTiles(snapshot: DashboardSnapshot | null): ClientMetricTile[] {
    if (snapshot === null) {
        return [];
    }

    return [
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.CALORIES',
            value: formatNumber(snapshot.statistics.totalCalories, ' kcal'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.PROTEIN',
            value: formatNumber(snapshot.statistics.averageProteins, ' g'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.FATS',
            value: formatNumber(snapshot.statistics.averageFats, ' g'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.CARBS',
            value: formatNumber(snapshot.statistics.averageCarbs, ' g'),
        },
    ];
}

export function buildBodyTiles(snapshot: DashboardSnapshot | null, permissions?: DietologistPermissions): ClientMetricTile[] {
    if (snapshot === null) {
        return [];
    }

    return [
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WEIGHT',
            value: formatNullableNumber(snapshot.weight.latest?.weight, ' kg'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WAIST',
            value: formatNullableNumber(snapshot.waist.latest?.circumference, ' cm'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.HYDRATION',
            value: formatNullableNumber(snapshot.hydration?.totalMl, ' ml'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.MEALS',
            value: String(snapshot.meals.total),
        },
    ].filter(tile => {
        if (permissions === undefined) {
            return true;
        }

        return (
            (tile.labelKey === 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WEIGHT' && permissions.shareWeight) ||
            (tile.labelKey === 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WAIST' && permissions.shareWaist) ||
            (tile.labelKey === 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.HYDRATION' && permissions.shareHydration) ||
            (tile.labelKey === 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.MEALS' && permissions.shareMeals)
        );
    });
}

export function buildGoalTiles(goals: DietologistClientGoals | null): ClientMetricTile[] {
    if (goals === null) {
        return [];
    }

    return [
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.CALORIE_GOAL',
            value: formatNullableNumber(goals.dailyCalorieTarget, ' kcal'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.PROTEIN_GOAL',
            value: formatNullableNumber(goals.proteinTarget, ' g'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WATER_GOAL',
            value: formatNullableNumber(goals.hydrationGoal ?? goals.waterGoal, ' ml'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.DESIRED_WEIGHT',
            value: formatNullableNumber(goals.desiredWeight, ' kg'),
        },
    ];
}

export function buildRecommendationViews(recommendations: DietologistRecommendation[]): ClientRecommendationView[] {
    return recommendations.map(recommendation => ({
        id: recommendation.id,
        text: recommendation.text,
        createdAtUtc: recommendation.createdAtUtc,
        statusKey: recommendation.isRead
            ? 'DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.READ'
            : 'DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.UNREAD',
    }));
}

export function buildMealViews(snapshot: DashboardSnapshot | null): ClientMealView[] {
    if (snapshot === null) {
        return [];
    }

    return snapshot.meals.items.map(meal => ({
        id: meal.id,
        title: formatMealTitle(meal),
        date: meal.date,
        calories: formatNumber(meal.totalCalories, ' kcal'),
        macros: `P ${formatNumber(meal.totalProteins, ' g')} / F ${formatNumber(meal.totalFats, ' g')} / C ${formatNumber(meal.totalCarbs, ' g')}`,
        itemSummary: formatMealItems(meal),
    }));
}

export function buildWeightView(snapshot: DashboardSnapshot | null): ClientBodyMeasurementView | null {
    if (snapshot?.weight.latest === null || snapshot?.weight.latest === undefined) {
        return null;
    }

    return {
        date: snapshot.weight.latest.date,
        value: formatNumber(snapshot.weight.latest.weight, ' kg'),
        delta: formatDelta(snapshot.weight.latest.weight, snapshot.weight.previous?.weight, ' kg'),
    };
}

export function buildWaistView(snapshot: DashboardSnapshot | null): ClientBodyMeasurementView | null {
    if (snapshot?.waist.latest === null || snapshot?.waist.latest === undefined) {
        return null;
    }

    return {
        date: snapshot.waist.latest.date,
        value: formatNumber(snapshot.waist.latest.circumference, ' cm'),
        delta: formatDelta(snapshot.waist.latest.circumference, snapshot.waist.previous?.circumference, ' cm'),
    };
}

export function buildHydrationView(snapshot: DashboardSnapshot | null): ClientHydrationView | null {
    if (snapshot?.hydration === null || snapshot?.hydration === undefined) {
        return null;
    }

    const goal = snapshot.hydration.goalMl;
    return {
        total: formatNumber(snapshot.hydration.totalMl, ' ml'),
        goal: goal === null ? null : formatNumber(goal, ' ml'),
        progress: goal === null || goal <= 0 ? null : Math.min(PERCENT_MAX, Math.round((snapshot.hydration.totalMl / goal) * PERCENT_MAX)),
    };
}

export function buildFastingView(snapshot: DashboardSnapshot | null): ClientFastingView | null {
    if (snapshot?.currentFastingSession === null || snapshot?.currentFastingSession === undefined) {
        return null;
    }

    const session = snapshot.currentFastingSession;
    return {
        status: session.status,
        protocol: formatFastingProtocol(session),
        startedAtUtc: session.startedAtUtc,
        plannedDuration: formatNumber(session.plannedDurationHours, ' h'),
        checkInSummary: formatFastingCheckIn(session),
    };
}

function formatProfileChip(value: number | null | undefined, suffix: string): string | null {
    return value === null || value === undefined ? null : `${value}${suffix}`;
}

function formatNullableNumber(value: number | null | undefined, suffix: string): string {
    return value === null || value === undefined ? '-' : formatNumber(value, suffix);
}

function formatNumber(value: number, suffix: string): string {
    return `${Math.round(value)}${suffix}`;
}

function formatDateOnly(value: string | null | undefined): string {
    if (value === null || value === undefined || value.length === 0) {
        return '-';
    }

    return value.slice(0, DATE_ONLY_LENGTH);
}

function formatMealTitle(meal: Meal): string {
    const mealType = meal.mealType?.trim();
    if (mealType !== undefined && mealType.length > 0) {
        return mealType;
    }

    const comment = meal.comment?.trim();
    return comment !== undefined && comment.length > 0 ? comment : 'Meal';
}

function formatMealItems(meal: Meal): string {
    const names = meal.items
        .map(item => item.product?.name ?? item.recipe?.name ?? null)
        .filter((value): value is string => value !== null && value.trim().length > 0);

    if (names.length === 0) {
        return `${meal.items.length} item${meal.items.length === 1 ? '' : 's'}`;
    }

    return names.slice(0, MEAL_ITEM_PREVIEW_LIMIT).join(', ');
}

function formatDelta(current: number, previous: number | null | undefined, suffix: string): string | null {
    if (previous === null || previous === undefined) {
        return null;
    }

    const delta = current - previous;
    if (Math.abs(delta) < STABLE_DELTA_THRESHOLD) {
        return `0${suffix}`;
    }

    return `${delta > 0 ? '+' : ''}${Math.round(delta * ONE_DECIMAL_PRECISION) / ONE_DECIMAL_PRECISION}${suffix}`;
}

function formatFastingProtocol(session: FastingSession): string {
    if (session.protocol.length > 0) {
        return session.protocol;
    }

    return session.planType;
}

function formatFastingCheckIn(session: FastingSession): string | null {
    if (session.checkInAtUtc === null) {
        return null;
    }

    return `Hunger ${session.hungerLevel ?? '-'} / Energy ${session.energyLevel ?? '-'} / Mood ${session.moodLevel ?? '-'}`;
}
