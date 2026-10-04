import { describe, expect, it } from 'vitest';

import { createClientValueFormatting, formatClientHeight } from './client-value-formatting';

const russianLabels: Record<string, string> = {
    'GENERAL.UNITS.CM': 'см',
    'GENERAL.UNITS.KG': 'кг',
    'GENERAL.UNITS.LB': 'фунт',
    'GENERAL.UNITS.FT': 'фут',
    'GENERAL.UNITS.IN': 'дюйм',
    'GENERAL.UNITS.KCAL': 'ккал',
    'GENERAL.UNITS.G': 'г',
    'GENERAL.UNITS.ML': 'мл',
    'GENERAL.UNITS.H': 'ч',
    'GENERAL.NUTRIENTS.PROTEIN': 'Белки',
    'GENERAL.NUTRIENTS.FAT': 'Жиры',
    'GENERAL.NUTRIENTS.CARB': 'Углеводы',
};
const values = {
    waist: 80.5,
    delta: -0.5,
    water: 17500,
    hours: 16,
    protein: 6.5,
    fat: 3.5,
    carbs: 30,
    height: 170,
    length: 1234.5,
    calories: 1900,
};

describe('client value formatting', () => {
    it('uses Russian decimal separators and translated units', () => {
        const formatting = createClientValueFormatting('ru', key => russianLabels[key]);

        expect(formatting.number(values.waist, 'cm', 1)).toBe('80,5 см');
        expect(formatting.number(values.delta, 'kg', 1)).toBe('−0,5 кг');
        expect(formatting.number(values.water, 'ml')).toBe('17\u00A0500 мл');
        expect(formatting.number(values.hours, 'h')).toBe('16 ч');
        expect(formatting.macros(values.protein, values.fat, values.carbs)).toBe('Белки 7 г / Жиры 4 г / Углеводы 30 г');
    });

    it('keeps height conversion and absent values intact', () => {
        const formatting = createClientValueFormatting('ru', key => russianLabels[key]);

        expect(formatClientHeight(values.height, 'metric', formatting)).toBe('170 см');
        expect(formatClientHeight(values.height, 'imperial', formatting)).toBe('5 фут 7 дюйм');
        expect(formatClientHeight(null, 'metric', formatting)).toBeNull();
    });

    it('uses English number grouping and decimal separators', () => {
        const formatting = createClientValueFormatting('en', key => key.split('.').at(-1)?.toLowerCase() ?? '');

        expect(formatting.number(values.length, 'cm', 1)).toBe('1,234.5 cm');
        expect(formatting.number(values.calories, 'kcal')).toBe('1,900 kcal');
    });
});
