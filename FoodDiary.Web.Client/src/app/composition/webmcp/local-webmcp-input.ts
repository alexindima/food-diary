import { MEAL_MANAGE_MIN_ITEM_AMOUNT } from '../../features/meals/lib/manage/meal-manage.config';
import { formatDateInputValue, parseLocalDateInputValue } from '../../shared/lib/local-date.utils';
import { MEAL_TYPE_OPTIONS } from '../../shared/lib/meal-type.util';
import { isRecord } from '../../shared/lib/unknown-value.utils';
import { MAXIMUM_CONSUMED_QUANTITY } from '../../shared/models/semantics/meal-quantity';
import { LocalWebmcpError } from './local-webmcp-error';

const MEAL_TYPES: readonly string[] = MEAL_TYPE_OPTIONS;
const MAX_ITEMS = 10;
const MAX_COMMENT_LENGTH = 1000;
const MAX_PRODUCT_NAME_LENGTH = 200;
const MAX_RECIPE_ID_LENGTH = 100;
type MealDraftInputItem = { productName: string; amount: number; unit: string } | { recipeId: string; servings: number };

export const DAY_SCHEMA = { type: 'string', pattern: '^\\d{4}-\\d{2}-\\d{2}$', description: 'Local calendar date, YYYY-MM-DD.' };
export const DRAFT_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    required: ['date', 'time', 'mealType', 'comment', 'items'],
    properties: {
        date: DAY_SCHEMA,
        time: { type: 'string', pattern: '^([01]\\d|2[0-3]):[0-5]\\d$', description: 'Local wall-clock time, HH:mm.' },
        mealType: { type: 'string', enum: MEAL_TYPES },
        comment: { type: 'string', maxLength: MAX_COMMENT_LENGTH },
        items: {
            type: 'array',
            minItems: 1,
            maxItems: MAX_ITEMS,
            items: {
                oneOf: [
                    {
                        type: 'object',
                        additionalProperties: false,
                        required: ['productName', 'amount', 'unit'],
                        properties: {
                            productName: {
                                type: 'string',
                                minLength: 1,
                                maxLength: MAX_PRODUCT_NAME_LENGTH,
                                description: 'Exact existing product name; ambiguous matches are rejected.',
                            },
                            amount: { type: 'number', minimum: MEAL_MANAGE_MIN_ITEM_AMOUNT, maximum: MAXIMUM_CONSUMED_QUANTITY },
                            unit: { type: 'string', description: 'Existing product baseUnit, e.g. G. No unit conversion is performed.' },
                        },
                    },
                    {
                        type: 'object',
                        additionalProperties: false,
                        required: ['recipeId', 'servings'],
                        properties: {
                            recipeId: { type: 'string', description: 'Exact accessible recipe ID.' },
                            servings: { type: 'number', minimum: MEAL_MANAGE_MIN_ITEM_AMOUNT, maximum: MAXIMUM_CONSUMED_QUANTITY },
                        },
                    },
                ],
            },
        },
    },
};

export function inputRecord(input: unknown, keys: string[]): Record<string, unknown> {
    if (!isRecord(input) || Object.keys(input).some(key => !keys.includes(key))) {
        throw new LocalWebmcpError('Invalid tool arguments.');
    }
    return input;
}

export function readDay(input: unknown): string {
    if (typeof input !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(input)) {
        throw new LocalWebmcpError('Expected calendar date YYYY-MM-DD.');
    }
    const day = parseLocalDateInputValue(input);
    if (day === null || formatDateInputValue(day) !== input) {
        throw new LocalWebmcpError('Invalid calendar date.');
    }
    return input;
}

export function readDraft(input: unknown): {
    date: string;
    time: string;
    mealType: string;
    comment: string;
    items: MealDraftInputItem[];
} {
    const value = inputRecord(input, ['date', 'time', 'mealType', 'comment', 'items']);
    const date = readDay(value['date']);
    const time = value['time'];
    const mealType = value['mealType'];
    const comment = value['comment'];
    if (
        typeof time !== 'string' ||
        typeof mealType !== 'string' ||
        typeof comment !== 'string' ||
        comment.length > MAX_COMMENT_LENGTH ||
        !/^([01]\d|2[0-3]):[0-5]\d$/.test(time) ||
        !MEAL_TYPES.includes(mealType)
    ) {
        throw new LocalWebmcpError('Invalid draft time, meal type or comment.');
    }
    if (!Array.isArray(value['items']) || value['items'].length === 0 || value['items'].length > MAX_ITEMS) {
        throw new LocalWebmcpError('Expected between 1 and 10 product items.');
    }
    requireLocalTime(date, time);
    const items = value['items'].map(readMealDraftItem);
    return { date, time, mealType, comment, items };
}

function readMealDraftItem(item: unknown): MealDraftInputItem {
    const row = inputRecord(item, ['productName', 'amount', 'unit', 'recipeId', 'servings']);
    if (row['recipeId'] !== undefined) {
        return readRecipeDraftItem(row);
    }
    if (row['servings'] !== undefined) {
        throw new LocalWebmcpError('Servings require recipeId.');
    }
    return readProductDraftItem(row);
}

function readRecipeDraftItem(row: Record<string, unknown>): MealDraftInputItem {
    const id = row['recipeId'];
    const servings = row['servings'];
    const conflicting = ['productName', 'amount', 'unit'].some(key => row[key] !== undefined);
    if (
        conflicting ||
        typeof id !== 'string' ||
        typeof servings !== 'number' ||
        id.length === 0 ||
        id.length > MAX_RECIPE_ID_LENGTH ||
        !Number.isFinite(servings) ||
        servings < MEAL_MANAGE_MIN_ITEM_AMOUNT ||
        servings > MAXIMUM_CONSUMED_QUANTITY
    ) {
        throw new LocalWebmcpError('Expected an exact recipe ID and a bounded serving quantity.');
    }
    return { recipeId: id, servings };
}

function readProductDraftItem(row: Record<string, unknown>): MealDraftInputItem {
    const productName = row['productName'];
    const amount = row['amount'];
    const unit = row['unit'];
    if (
        typeof productName !== 'string' ||
        typeof amount !== 'number' ||
        typeof unit !== 'string' ||
        amount < MEAL_MANAGE_MIN_ITEM_AMOUNT ||
        amount > MAXIMUM_CONSUMED_QUANTITY ||
        productName.length > MAX_PRODUCT_NAME_LENGTH ||
        productName.trim().length === 0 ||
        !Number.isFinite(amount)
    ) {
        throw new LocalWebmcpError('Invalid product item.');
    }
    return { productName: productName.trim(), amount, unit };
}

function requireLocalTime(date: string, time: string): void {
    const localTime = new Date(`${date}T${time}`);
    const [hour, minute] = time.split(':').map(Number);
    if (formatDateInputValue(localTime) !== date || localTime.getHours() !== hour || localTime.getMinutes() !== minute) {
        throw new LocalWebmcpError('Local time does not exist in this timezone.');
    }
}
