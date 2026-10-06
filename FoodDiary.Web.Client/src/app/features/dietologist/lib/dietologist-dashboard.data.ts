import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import type { Meal, MealResponseDto } from '../../../shared/models/meal.data';

export type DietologistDashboardSnapshot = DashboardSnapshot<Meal | MealResponseDto>;
