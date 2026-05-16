import type { FoodNutritionResponse, FoodVisionItem } from '../models/ai.data';
import { normalizeAiNutritionItemName } from './ai-nutrition-edit.utils';

export type AiEditableFoodItem = {
    id: string;
    name: string;
    nameEn: string;
    nameLocal: string | null;
    amount: number;
    unit: string;
};

export type AiEditableItemUpdateField = 'name' | 'amount' | 'unit';

const AI_PHOTO_UNIT_TRANSLATION_KEYS: Readonly<Record<string, string>> = {
    g: 'GENERAL.UNITS.G',
    gram: 'GENERAL.UNITS.G',
    grams: 'GENERAL.UNITS.G',
    gr: 'GENERAL.UNITS.G',
    ml: 'GENERAL.UNITS.ML',
    l: 'GENERAL.UNITS.ML',
    pcs: 'GENERAL.UNITS.PCS',
    pc: 'GENERAL.UNITS.PCS',
    piece: 'GENERAL.UNITS.PCS',
    pieces: 'GENERAL.UNITS.PCS',
    kcal: 'GENERAL.UNITS.KCAL',
};

export function resolveAiPhotoUnitKey(unit?: string | null): string | null {
    if (unit === null || unit === undefined) {
        return null;
    }

    return AI_PHOTO_UNIT_TRANSLATION_KEYS[unit.trim().toLowerCase()] ?? null;
}

export function buildAiEditableItems(
    results: readonly FoodVisionItem[],
    nutrition: FoodNutritionResponse | null,
    createId: () => string,
): AiEditableFoodItem[] {
    const items =
        results.length > 0
            ? results
            : (nutrition?.items.map(item => ({
                  nameEn: item.name,
                  nameLocal: null,
                  amount: item.amount,
                  unit: item.unit,
                  confidence: 1,
              })) ?? []);

    return items.map(item => {
        const nameEn = item.nameEn.trim();
        const localName = item.nameLocal?.trim() ?? '';

        return {
            id: createId(),
            name: localName.length > 0 ? localName : nameEn,
            nameEn,
            nameLocal: localName.length > 0 ? localName : null,
            amount: item.amount,
            unit: item.unit,
        };
    });
}

export function normalizeAiEditableItems(edited: readonly AiEditableFoodItem[]): FoodVisionItem[] {
    return edited.map(item => {
        const localName = item.nameLocal?.trim() ?? '';
        return {
            nameEn: item.nameEn.trim().length > 0 ? item.nameEn.trim() : item.name.trim(),
            nameLocal: localName.length > 0 ? localName : null,
            amount: item.amount,
            unit: item.unit,
            confidence: 1,
        };
    });
}

export function requiresAiNutritionRecalculation(source: readonly AiEditableFoodItem[], edited: readonly AiEditableFoodItem[]): boolean {
    const sourceById = new Map(source.map(item => [item.id, item]));

    if (edited.some(item => !sourceById.has(item.id))) {
        return true;
    }

    return edited.some(item => {
        const previous = sourceById.get(item.id);
        if (previous === undefined) {
            return false;
        }

        const nameChanged = normalizeAiNutritionItemName(previous.name) !== normalizeAiNutritionItemName(item.name);
        const unitChanged = previous.unit.trim().toLowerCase() !== item.unit.trim().toLowerCase();
        return nameChanged || unitChanged;
    });
}

export function updateAiEditableItem(
    items: readonly AiEditableFoodItem[],
    index: number,
    field: AiEditableItemUpdateField,
    value: string,
): AiEditableFoodItem[] {
    return items.map((item, idx) => {
        if (idx !== index) {
            return item;
        }

        if (field === 'amount') {
            const parsed = Number.parseFloat(value);
            return { ...item, amount: Number.isNaN(parsed) ? 0 : parsed };
        }

        if (field === 'unit') {
            return { ...item, unit: value };
        }

        return { ...item, name: value, nameEn: value, nameLocal: value };
    });
}

export function createEmptyAiEditableItem(createId: () => string, unit: string): AiEditableFoodItem {
    return { id: createId(), name: '', nameEn: '', nameLocal: '', amount: 0, unit };
}
