import type { MealCardItem } from '../meal-card/meal-card.component';

export interface MealPreviewEntry {
    meal?: MealCardItem | null;
    slot?: string | null;
    icon?: string;
    labelKey?: string;
}
