import type { AiEditableFoodItem } from '../../../../../shared/lib/ai-photo-edit.utils';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../../shared/models/ai.data';

export type EditableAiItem = AiEditableFoodItem;

export type AiResultRow = {
    key: string;
    displayName: string;
    amountLabel: string;
};

export type AiNutritionSummaryItem = {
    labelKey: string;
    value: string;
};

export type AiEditUnitOption = {
    value: string;
    label: string;
};

export type AiEditActionView = {
    variant: 'primary' | 'secondary';
    fill: 'solid' | 'outline';
    labelKey: string;
};

export type AiDetailsToggleView = {
    icon: string;
    labelKey: string;
};

export type AiEditItemUpdate = {
    index: number;
    field: 'name' | 'amount' | 'unit';
    value: string;
};

export type AiEditItemDrop = {
    previousIndex: number;
    currentIndex: number;
};

export type AiPhotoEditApplied = {
    items: FoodVisionItem[];
    nutrition: FoodNutritionResponse | null;
};
