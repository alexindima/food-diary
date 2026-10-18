import type { DietologistDashboardSnapshot } from '../../../lib/dietologist-dashboard.data';

export function appendClientMealPage(
    snapshot: DietologistDashboardSnapshot,
    page: DietologistDashboardSnapshot,
): DietologistDashboardSnapshot {
    const existingIds = new Set(snapshot.meals.items.map(meal => meal.id));
    return {
        ...snapshot,
        meals: {
            total: page.meals.total,
            items: [...snapshot.meals.items, ...page.meals.items.filter(meal => !existingIds.has(meal.id))],
        },
    };
}
