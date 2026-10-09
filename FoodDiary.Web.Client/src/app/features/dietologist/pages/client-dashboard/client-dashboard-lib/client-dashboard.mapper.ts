import { normalizeMealType } from '../../../../../shared/lib/meal-type.util';
import {
    centimetersToInches,
    kilogramsToPounds,
    type MeasurementSystem,
} from '../../../../../shared/measurements/measurement-system.service';
import type { ClientSummary, DietologistPermissions } from '../../../../../shared/models/dietologist.data';
import type { DietologistClientGoals, DietologistRecommendation } from '../../../../../shared/models/dietologist.data';
import type { FastingSession } from '../../../../../shared/models/fasting.data';
import type { Meal, MealItem, MealItemResponseDto, MealResponseDto } from '../../../../../shared/models/meal.data';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { type ClientValueFormatting, DEFAULT_CLIENT_VALUE_FORMATTING, formatClientHeight } from '../../../lib/client-value-formatting';
import type { DietologistDashboardSnapshot } from '../../../lib/dietologist-dashboard.data';

const PERCENT_MAX = 100;
const DATE_ONLY_LENGTH = 10;
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

export function getClientDashboardTitle(client: ClientSummary): string | null {
    const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
    return fullName.length > 0 ? fullName : client.email;
}

export function buildClientProfileChips(
    client: ClientSummary | null,
    system: MeasurementSystem = 'metric',
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): string[] {
    if (client?.permissions.shareProfile !== true) {
        return [];
    }

    return [formatClientHeight(client.heightCm, system, formatting), client.gender, client.activityLevel].filter(
        (value): value is string => value !== null && value.length > 0,
    );
}

export function buildClientProfileDetails(
    client: ClientSummary | null,
    system: MeasurementSystem = 'metric',
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientProfileDetail[] {
    if (client?.permissions.shareProfile !== true) {
        return [];
    }

    return [
        { labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.PROFILE.EMAIL', value: client.email ?? '-' },
        { labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.PROFILE.HEIGHT', value: formatClientHeight(client.heightCm, system, formatting) ?? '-' },
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

export function buildNutritionTiles(
    snapshot: DietologistDashboardSnapshot | null,
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientMetricTile[] {
    if (snapshot === null) {
        return [];
    }

    return [
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.CALORIES',
            value: formatting.number(snapshot.statistics.totalCalories, 'kcal'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.PROTEIN',
            value: formatting.number(snapshot.statistics.averageProteins, 'g'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.FATS',
            value: formatting.number(snapshot.statistics.averageFats, 'g'),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.CARBS',
            value: formatting.number(snapshot.statistics.averageCarbs, 'g'),
        },
    ];
}

export function buildBodyTiles(
    snapshot: DietologistDashboardSnapshot | null,
    permissions?: DietologistPermissions,
    system: MeasurementSystem = 'metric',
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientMetricTile[] {
    if (snapshot === null) {
        return [];
    }

    return [
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WEIGHT',
            value: formatMeasurement(snapshot.weight.latest?.weightKg, system, 'weight', formatting),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WAIST',
            value: formatMeasurement(snapshot.waist.latest?.circumferenceCm, system, 'length', formatting),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.HYDRATION',
            value: formatNullableNumber(snapshot.hydration?.totalMl, 'ml', formatting),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.MEALS',
            value: formatting.number(snapshot.meals.total),
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

export function buildGoalTiles(
    goals: DietologistClientGoals | null,
    system: MeasurementSystem = 'metric',
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientMetricTile[] {
    if (goals === null) {
        return [];
    }

    return [
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.CALORIE_GOAL',
            value: formatNullableNumber(goals.dailyCalorieTarget, 'kcal', formatting),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.PROTEIN_GOAL',
            value: formatNullableNumber(goals.proteinTarget, 'g', formatting),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.WATER_GOAL',
            value: formatNullableNumber(goals.hydrationGoal ?? goals.waterGoal, 'ml', formatting),
        },
        {
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.DESIRED_WEIGHT',
            value: formatMeasurement(goals.desiredWeightKg, system, 'weight', formatting),
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

export function buildMealViews(
    snapshot: DietologistDashboardSnapshot | null,
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
    translate: (key: string) => string = key => key,
    language = 'en',
): ClientMealView[] {
    if (snapshot === null) {
        return [];
    }

    return snapshot.meals.items.map(meal => ({
        id: entityId<'meal'>(meal.id),
        title: translate(`MEAL_TYPES.${normalizeMealType(meal.mealType) ?? 'OTHER'}`),
        date: utcInstant(meal.date),
        calories: formatting.number(meal.totalCalories, 'kcal'),
        macros: formatting.macros(meal.totalProteins, meal.totalFats, meal.totalCarbs),
        itemSummary: formatMealItems(meal, formatting, translate, language),
    }));
}

export function buildWeightView(
    snapshot: DietologistDashboardSnapshot | null,
    system: MeasurementSystem = 'metric',
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientBodyMeasurementView | null {
    if (snapshot?.weight.latest === null || snapshot?.weight.latest === undefined) {
        return null;
    }

    return {
        date: snapshot.weight.latest.date,
        value: formatMeasurement(snapshot.weight.latest.weightKg, system, 'weight', formatting),
        delta: formatMeasurementDelta(
            { current: snapshot.weight.latest.weightKg, previous: snapshot.weight.previous?.weightKg },
            system,
            'weight',
            formatting,
        ),
    };
}

export function buildWaistView(
    snapshot: DietologistDashboardSnapshot | null,
    system: MeasurementSystem = 'metric',
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientBodyMeasurementView | null {
    if (snapshot?.waist.latest === null || snapshot?.waist.latest === undefined) {
        return null;
    }

    return {
        date: snapshot.waist.latest.date,
        value: formatMeasurement(snapshot.waist.latest.circumferenceCm, system, 'length', formatting),
        delta: formatMeasurementDelta(
            { current: snapshot.waist.latest.circumferenceCm, previous: snapshot.waist.previous?.circumferenceCm },
            system,
            'length',
            formatting,
        ),
    };
}

export function buildHydrationView(
    snapshot: DietologistDashboardSnapshot | null,
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientHydrationView | null {
    if (snapshot?.hydration === null || snapshot?.hydration === undefined) {
        return null;
    }

    const goal = snapshot.hydration.goalMl;
    return {
        total: formatting.number(snapshot.hydration.totalMl, 'ml'),
        goal: goal === null ? null : formatting.number(goal, 'ml'),
        progress: goal === null || goal <= 0 ? null : Math.min(PERCENT_MAX, Math.round((snapshot.hydration.totalMl / goal) * PERCENT_MAX)),
    };
}

export function buildFastingView(
    snapshot: DietologistDashboardSnapshot | null,
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): ClientFastingView | null {
    if (snapshot?.currentFastingSession === null || snapshot?.currentFastingSession === undefined) {
        return null;
    }

    const session = snapshot.currentFastingSession;
    return {
        status: session.status,
        protocol: formatFastingProtocol(session),
        startedAtUtc: session.startedAtUtc,
        plannedDuration: formatting.number(session.plannedDurationHours, 'h'),
        checkInSummary: formatFastingCheckIn(session),
    };
}

function formatMeasurement(
    value: number | null | undefined,
    system: MeasurementSystem,
    kind: 'weight' | 'length',
    formatting: ClientValueFormatting,
): string {
    if (value === null || value === undefined) {
        return '-';
    }

    const converted = convertMeasurement(value, system, kind);
    return formatting.number(converted, measurementUnit(system, kind), 1);
}

function formatMeasurementDelta(
    measurement: { current: number; previous: number | null | undefined },
    system: MeasurementSystem,
    kind: 'weight' | 'length',
    formatting: ClientValueFormatting,
): string | null {
    if (measurement.previous === null || measurement.previous === undefined) {
        return null;
    }

    const delta = convertMeasurement(measurement.current - measurement.previous, system, kind);
    const roundedDelta = Math.round(delta * ONE_DECIMAL_PRECISION) / ONE_DECIMAL_PRECISION;
    const normalizedDelta = Math.abs(roundedDelta) < STABLE_DELTA_THRESHOLD ? 0 : roundedDelta;
    return `${normalizedDelta > 0 ? '+' : ''}${formatting.number(normalizedDelta, measurementUnit(system, kind), 1)}`;
}

function convertMeasurement(value: number, system: MeasurementSystem, kind: 'weight' | 'length'): number {
    if (system === 'metric') {
        return Math.round(value * ONE_DECIMAL_PRECISION) / ONE_DECIMAL_PRECISION;
    }

    return kind === 'weight' ? kilogramsToPounds(value) : centimetersToInches(value);
}

function measurementUnit(system: MeasurementSystem, kind: 'weight' | 'length'): 'kg' | 'lb' | 'cm' | 'in' {
    if (kind === 'weight') {
        return system === 'imperial' ? 'lb' : 'kg';
    }

    return system === 'imperial' ? 'in' : 'cm';
}

function formatNullableNumber(
    value: number | null | undefined,
    unit: Parameters<ClientValueFormatting['number']>[1],
    formatting: ClientValueFormatting,
): string {
    return value === null || value === undefined ? '-' : formatting.number(value, unit);
}

function formatDateOnly(value: string | null | undefined): string {
    if (value === null || value === undefined || value.length === 0) {
        return '-';
    }

    return value.slice(0, DATE_ONLY_LENGTH);
}

function formatMealItems(
    meal: Meal | MealResponseDto,
    formatting: ClientValueFormatting,
    translate: (key: string) => string,
    language: string,
): string {
    const names = meal.items.map(item => formatMealItem(item, formatting, translate, language)).filter(Boolean);
    const resolvedAiIds = new Set(meal.items.map(item => item.sourceAiItemId));
    const aiNames = (meal.aiSessions ?? [])
        .flatMap(session => session.items)
        .filter(item => !resolvedAiIds.has(item.id))
        .map(
            item =>
                `${item.nameLocal ?? item.nameEn} — ${formatting.number(item.amount, undefined, 1)} ${translate(`GENERAL.UNITS.${item.unit.toUpperCase()}`)}`,
        );
    const summary = [...names, ...aiNames].join(', ');
    return summary.length > 0 ? summary : '-';
}

function formatMealItem(
    item: MealItem | MealItemResponseDto,
    formatting: ClientValueFormatting,
    translate: (key: string) => string,
    language: string,
): string {
    const name = getMealItemName(item);
    if ((name?.trim().length ?? 0) === 0) {
        return '';
    }
    const unit = getMealItemUnit(item, language);
    return `${name} — ${formatting.number(item.amount, undefined, 1)} ${translate(unit)}`;
}

function getMealItemName(item: MealItem | MealItemResponseDto): string | null | undefined {
    return 'sourceType' in item ? (item.product?.name ?? item.recipe?.name) : (item.productName ?? item.recipeName);
}

function getMealItemUnit(item: MealItem | MealItemResponseDto, language: string): string {
    const recipeId = 'sourceType' in item ? item.recipe?.id : item.recipeId;
    if (Boolean(recipeId)) {
        const category = new Intl.PluralRules(language, { maximumFractionDigits: 1 }).select(item.amount);
        return `QUICK_MEAL.SERVINGS_${category.toUpperCase()}`;
    }
    const unit = 'sourceType' in item ? item.product?.baseUnit : item.productBaseUnit;
    return `GENERAL.UNITS.${unit?.toUpperCase() ?? 'G'}`;
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

    return `${session.hungerLevel ?? '-'} / ${session.energyLevel ?? '-'} / ${session.moodLevel ?? '-'}`;
}
