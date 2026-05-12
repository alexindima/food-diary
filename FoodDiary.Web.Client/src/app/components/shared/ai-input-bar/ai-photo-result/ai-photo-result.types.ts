import type { FoodVisionItem } from '../../../../shared/models/ai.data';

export type EditableAiItem = {
    id: string;
    name: string;
    nameEn: string;
    nameLocal: string | null;
    amount: number;
    unit: string;
};

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

export type AiEditedItemsView = {
    items: EditableAiItem[];
    unitOptions: AiEditUnitOption[];
};

export type AiDetectedItemsView = {
    rows: AiResultRow[];
    sourceItems: FoodVisionItem[];
};
