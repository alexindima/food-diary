import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import type { Meal, MealItem } from '../../../shared/models/meal.data';
import type { NutritionMetric, NutritionSource } from '../models/nutrition-details.types';

const TOTAL_ROUNDING_TOLERANCE = 0.011;

const METRIC_FIELDS = {
    calories: {
        total: 'totalCalories',
        product: 'caloriesPerBase',
        ai: 'calories',
        label: 'GENERAL.NUTRIENTS.CALORIES',
        color: '--fd-color-text',
    },
    protein: {
        total: 'totalProteins',
        product: 'proteinsPerBase',
        ai: 'proteins',
        label: 'GENERAL.NUTRIENTS.PROTEIN',
        color: '--fd-color-nutrition-proteins',
    },
    fats: { total: 'totalFats', product: 'fatsPerBase', ai: 'fats', label: 'GENERAL.NUTRIENTS.FAT', color: '--fd-color-amber-500' },
    carbs: {
        total: 'totalCarbs',
        product: 'carbsPerBase',
        ai: 'carbs',
        label: 'GENERAL.NUTRIENTS.CARB',
        color: '--fd-color-nutrition-carbs',
    },
    fiber: {
        total: 'totalFiber',
        product: 'fiberPerBase',
        ai: 'fiber',
        label: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
        color: '--fd-color-rose-500',
    },
} as const;

export function nutritionMetricPresentation(metric: NutritionMetric): { labelKey: string; unitKey: string; color: string } {
    return {
        labelKey: METRIC_FIELDS[metric].label,
        unitKey: metric === 'calories' ? 'MEAL_RING.UNIT' : 'GENERAL.UNITS.G',
        color: `var(${METRIC_FIELDS[metric].color})`,
    };
}

export function nutritionDayProgress(snapshot: DashboardSnapshot, metric: NutritionMetric): { current: number; goal: number | null } {
    const statistics = snapshot.statistics;
    switch (metric) {
        case 'calories': {
            return { current: statistics.totalCalories, goal: snapshot.dailyGoal };
        }
        case 'protein': {
            return { current: statistics.averageProteins, goal: statistics.proteinGoal ?? null };
        }
        case 'fats': {
            return { current: statistics.averageFats, goal: statistics.fatGoal ?? null };
        }
        case 'carbs': {
            return { current: statistics.averageCarbs, goal: statistics.carbGoal ?? null };
        }
        case 'fiber': {
            return { current: statistics.averageFiber, goal: statistics.fiberGoal ?? null };
        }
    }
}

export function buildNutritionSources(meals: readonly Meal[], metric: NutritionMetric, language: string): NutritionSource[] {
    return meals.flatMap(meal => buildMealSources(meal, metric, language)).sort((left, right) => right.value - left.value);
}

function buildMealSources(meal: Meal, metric: NutritionMetric, language: string): NutritionSource[] {
    const total = meal[METRIC_FIELDS[metric].total];
    if (total <= 0) {
        return [];
    }
    const fallback = (kind: NutritionSource['kind']): NutritionSource[] => [{ id: meal.id, meal, name: null, kind, value: total }];
    if (metric === 'calories') {
        return fallback(meal.isNutritionAutoCalculated ? 'meal' : 'manual');
    }
    if (!meal.isNutritionAutoCalculated) {
        return fallback('manual');
    }

    // Read the saved projection only; never resolve today's product/recipe in a historical meal.
    const items = meal.items.map(item => itemSource(meal, item, metric));
    const sources = items.filter((source): source is NutritionSource => source !== null);
    sources.push(...aiSources(meal, metric, language));
    const sum = sources.reduce((value, source) => value + source.value, 0);
    // Stored meal totals round to two decimals. An incomplete or changed projection must
    // not masquerade as a complete product breakdown or redistribute manual differences.
    if (items.includes(null) || !Number.isFinite(sum) || Math.abs(sum - total) > TOTAL_ROUNDING_TOLERANCE) {
        return fallback('unavailable');
    }
    return sources.filter(source => source.value > 0);
}

function itemSource(meal: Meal, item: MealItem, metric: NutritionMetric): NutritionSource | null {
    const fields = METRIC_FIELDS[metric];
    if (item.product !== null) {
        const product = item.product;
        if (product.baseAmount <= 0) {
            return null;
        }
        return {
            id: `${meal.id}:${item.id}`,
            meal,
            name: product.name,
            kind: 'product',
            value: (product[fields.product] * item.amount) / product.baseAmount,
        };
    }
    if (
        item.recipe === null ||
        item.recipe.servings <= 0 ||
        item.recipe[fields.total] === null ||
        item.recipe[fields.total] === undefined
    ) {
        return null;
    }
    return {
        id: `${meal.id}:${item.id}`,
        meal,
        name: item.recipe.name,
        kind: 'recipe',
        value: ((item.recipe[fields.total] ?? 0) * item.amount) / item.recipe.servings,
    };
}

function aiSources(meal: Meal, metric: NutritionMetric, language: string): NutritionSource[] {
    const sources: NutritionSource[] = [];
    for (const session of meal.aiSessions ?? []) {
        for (const item of session.items) {
            if (item.resolution === 'Rejected') {
                continue;
            }
            sources.push({
                id: `${meal.id}:ai:${item.id}`,
                meal,
                name: language === 'ru' ? (item.nameLocal ?? item.nameEn) : item.nameEn,
                kind: 'ai',
                value: item[METRIC_FIELDS[metric].ai],
            });
        }
    }
    return sources;
}
