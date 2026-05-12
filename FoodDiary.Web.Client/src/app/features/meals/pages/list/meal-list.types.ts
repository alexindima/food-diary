import type { FavoriteMeal, Meal } from '../../models/meal.data';

export type FavoriteMealView = {
    favorite: FavoriteMeal;
    displayName: string | null;
    displayNameKey: string;
};

export type MealDateGroupView = {
    date: Date;
    dateLabel: string;
    items: Meal[];
};
