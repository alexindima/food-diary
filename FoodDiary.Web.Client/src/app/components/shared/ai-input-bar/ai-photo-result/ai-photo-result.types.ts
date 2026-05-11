import type { FoodVisionItem } from '../../../../shared/models/ai.data';

export type EditableAiItem = {
    id: string;
    name: string;
    nameEn: string;
    nameLocal: string | null;
    amount: number;
    unit: string;
};

export interface AiResultRow {
    key: string;
    displayName: string;
    amountLabel: string;
}

export interface AiNutritionSummaryItem {
    labelKey: string;
    value: string;
}

export interface AiEditUnitOption {
    value: string;
    label: string;
}

export interface AiEditActionView {
    variant: 'primary' | 'secondary';
    fill: 'solid' | 'outline';
    labelKey: string;
}

export interface AiDetailsToggleView {
    icon: string;
    labelKey: string;
}

export interface AiEditItemUpdate {
    index: number;
    field: 'name' | 'amount' | 'unit';
    value: string;
}

export interface AiEditItemDrop {
    previousIndex: number;
    currentIndex: number;
}

export interface AiEditedItemsView {
    items: EditableAiItem[];
    unitOptions: AiEditUnitOption[];
}

export interface AiDetectedItemsView {
    rows: AiResultRow[];
    sourceItems: FoodVisionItem[];
}
