import { devEnum, devNumber, devRecord, devText } from '../../../../../shared/development/development-input';
import { MeasurementUnit } from '../../../../../shared/models/product.data';
import { PRODUCT_NAME_MAX_LENGTH } from '../../../lib/product-manage.constants';
import { createProductForm, getDefaultProductBaseAmount } from './product-manage-form.mapper';
import type { ProductFormValues } from './product-manage-form.types';

const NUTRIENTS = ['caloriesPerBase', 'proteinsPerBase', 'fatsPerBase', 'carbsPerBase', 'fiberPerBase', 'alcoholPerBase'] as const;
const MAX_INPUT_NUMBER = 1_000_000;
export const PRODUCT_DEVELOPMENT_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    required: ['name', 'baseUnit', 'defaultPortionAmount', 'caloriesPerBase'],
    properties: {
        name: { type: 'string', maxLength: PRODUCT_NAME_MAX_LENGTH },
        baseUnit: { enum: Object.values(MeasurementUnit) },
        defaultPortionAmount: { type: 'number' },
        ...Object.fromEntries(NUTRIENTS.map(field => [field, { type: 'number' }])),
    },
    description:
        'New PRIVATE product. Nutrients per 100 G/ML or 1 PCS. Missing macros default to zero. Existing form validation remains authoritative.',
};

export function readProductDevelopmentDraft(input: unknown): ProductFormValues {
    const value = devRecord(input, ['name', 'baseUnit', 'defaultPortionAmount', ...NUTRIENTS]);
    const baseUnit = devEnum(value['baseUnit'], Object.values(MeasurementUnit));
    return {
        ...createProductForm(),
        name: devText(value['name'], PRODUCT_NAME_MAX_LENGTH, true),
        baseUnit,
        baseAmount: getDefaultProductBaseAmount(baseUnit),
        defaultPortionAmount: devNumber(value['defaultPortionAmount'], 0, MAX_INPUT_NUMBER),
        caloriesPerBase: devNumber(value['caloriesPerBase'], 0, MAX_INPUT_NUMBER),
        proteinsPerBase: devNumber(value['proteinsPerBase'] ?? 0, 0, MAX_INPUT_NUMBER),
        fatsPerBase: devNumber(value['fatsPerBase'] ?? 0, 0, MAX_INPUT_NUMBER),
        carbsPerBase: devNumber(value['carbsPerBase'] ?? 0, 0, MAX_INPUT_NUMBER),
        fiberPerBase: devNumber(value['fiberPerBase'] ?? 0, 0, MAX_INPUT_NUMBER),
        alcoholPerBase: devNumber(value['alcoholPerBase'] ?? 0, 0, MAX_INPUT_NUMBER),
    };
}
