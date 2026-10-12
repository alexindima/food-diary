import type { FoodNutritionResponse, FoodVisionItem } from '../models/ai.data';
import { normalizeAiNutritionItemName } from './ai-nutrition-edit.utils';

export type AiEditableFoodItem = {
    id: string;
    name: string;
    nameEn: string;
    nameLocal: string | null;
    amount: number;
    unit: string;
    confidence: number;
    resolution: AiEditableFoodItemResolution;
};

export type AiEditableFoodItemResolution = 'Candidate' | 'Accepted' | 'Replaced' | 'Rejected' | 'Split' | 'Merged';

export type AiEditableItemUpdateField = 'name' | 'amount' | 'unit' | 'resolution';

const AI_PHOTO_UNIT_TRANSLATION_KEYS: Readonly<Record<string, string>> = {
    g: 'GENERAL.UNITS.G',
    gram: 'GENERAL.UNITS.G',
    grams: 'GENERAL.UNITS.G',
    gr: 'GENERAL.UNITS.G',
    ml: 'GENERAL.UNITS.ML',
    pcs: 'GENERAL.UNITS.PCS',
    pc: 'GENERAL.UNITS.PCS',
    piece: 'GENERAL.UNITS.PCS',
    pieces: 'GENERAL.UNITS.PCS',
    item: 'GENERAL.UNITS.PCS',
    items: 'GENERAL.UNITS.PCS',
    шт: 'GENERAL.UNITS.PCS',
    'шт.': 'GENERAL.UNITS.PCS',
    kcal: 'GENERAL.UNITS.KCAL',
};

export function normalizeAiPhotoUnit(unit: string): string {
    const normalized = unit.trim().toLowerCase();
    const key = AI_PHOTO_UNIT_TRANSLATION_KEYS[normalized];
    if (key === 'GENERAL.UNITS.G') {
        return 'g';
    }
    if (key === 'GENERAL.UNITS.ML') {
        return 'ml';
    }
    if (key === 'GENERAL.UNITS.PCS') {
        return 'pcs';
    }
    return normalized;
}

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
                  unit: normalizeAiPhotoUnit(item.unit),
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
            unit: normalizeAiPhotoUnit(item.unit),
            confidence: item.confidence,
            resolution: 'Accepted',
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
            unit: normalizeAiPhotoUnit(item.unit),
            confidence: item.confidence,
        };
    });
}

export function aiEditableItemError(item: AiEditableFoodItem): 'name' | 'amount' | 'unit' | null {
    if (item.name.trim().length === 0) {
        return 'name';
    }
    if (!Number.isFinite(item.amount) || item.amount <= 0) {
        return 'amount';
    }
    return item.unit.trim().length === 0 ? 'unit' : null;
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
        const unitChanged = normalizeAiPhotoUnit(previous.unit) !== normalizeAiPhotoUnit(item.unit);
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

        if (field === 'resolution') {
            return isAiEditableFoodItemResolution(value) ? { ...item, resolution: value } : item;
        }

        return { ...item, name: value, nameEn: value, nameLocal: value };
    });
}

export function createEmptyAiEditableItem(createId: () => string, unit: string): AiEditableFoodItem {
    return { id: createId(), name: '', nameEn: '', nameLocal: '', amount: 0, unit, confidence: 1, resolution: 'Accepted' };
}

function isAiEditableFoodItemResolution(value: string): value is AiEditableFoodItemResolution {
    return (
        value === 'Candidate' ||
        value === 'Accepted' ||
        value === 'Replaced' ||
        value === 'Rejected' ||
        value === 'Split' ||
        value === 'Merged'
    );
}
