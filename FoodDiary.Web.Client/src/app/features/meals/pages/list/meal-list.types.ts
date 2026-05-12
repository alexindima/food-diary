import type { FavoriteMeal, Meal } from '../../models/meal.data';

export interface FavoriteMealView {
    favorite: FavoriteMeal;
    displayName: string | null;
    displayNameKey: string;
}

export interface MealDateGroupView {
    date: Date;
    dateLabel: string;
    items: Meal[];
}
