import type { AiEditableFoodItem } from '../../../../../shared/lib/ai-photo-edit.utils';
import type { FoodVisionItem } from '../../../../../shared/models/ai.data';

export type RecognizedItemView = {
    item: FoodVisionItem;
    displayName: string;
    amount: number;
    unit: string | null | undefined;
    unitKey: string | null;
};

export type UnitOptionView = {
    value: string;
    labelKey: string;
};

export type MacroSummaryItem = {
    key: 'calories' | 'protein' | 'fat' | 'carbs' | 'fiber' | 'alcohol';
    labelKey: string;
    value: number;
    unitKey: string;
    numberFormat: string;
};

export type EditableAiItem = AiEditableFoodItem;

export type PhotoAiEditItemDrop = {
    previousIndex: number;
    currentIndex: number;
};

export type PhotoAiEditItemUpdate = {
    index: number;
    field: 'name' | 'amount' | 'unit';
    value: string;
};
