import type { AiEditableFoodItem, AiEditableItemUpdateField } from '../../../../../shared/lib/ai-photo-edit.utils';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../../shared/models/ai.data';

export type EditableAiItem = AiEditableFoodItem;

export type AiPhotoConnectorPoint = {
    x: number;
    y: number;
};

export type AiPhotoAnnotation = {
    id: string;
    name: string;
    amountLabel: string;
    centerX: number;
    centerY: number;
    cardX: number;
    cardY: number;
    cardWidth: number;
    cardHeight: number;
    connectorPoints: readonly [AiPhotoConnectorPoint, AiPhotoConnectorPoint];
    connectorPath: string;
    calories: number;
    protein: number;
    fat: number;
    carbs: number;
};

export type AiResultRow = {
    key: string;
    annotationId: string | null;
    displayName: string;
    amountLabel: string;
    calories: number | null;
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
    field: AiEditableItemUpdateField;
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
