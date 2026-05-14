import { describe, expect, it } from 'vitest';

import type { DailyMicronutrient, Micronutrient } from '../models/usda.data';
import { buildDailyMicronutrientViews, buildMineralMicronutrientViews, buildVitaminMicronutrientViews } from './usda-micronutrient.mapper';
import { USDA_NUTRIENT_IDS } from './usda-nutrient.constants';

const CLAMPED_PERCENT_WIDTH = 100;
const NEGATIVE_PERCENT = -10;
const ABOVE_MAX_PERCENT = 125;
const VITAMIN_C_AMOUNT = 24;
const CALCIUM_AMOUNT = 120;
const ENERGY_AMOUNT = 50;
const ZINC_TOTAL_AMOUNT = 4;
const ZINC_PERCENT_DAILY_VALUE = 35;

describe('USDA micronutrient mapper', () => {
    it('builds vitamin and mineral views with clamped percent width', () => {
        const nutrients: Micronutrient[] = [
            {
                nutrientId: USDA_NUTRIENT_IDS.vitaminC,
                name: 'Vitamin C',
                unit: 'mg',
                amountPer100g: VITAMIN_C_AMOUNT,
                dailyValue: null,
                percentDailyValue: ABOVE_MAX_PERCENT,
            },
            {
                nutrientId: USDA_NUTRIENT_IDS.calcium,
                name: 'Calcium',
                unit: 'mg',
                amountPer100g: CALCIUM_AMOUNT,
                dailyValue: null,
                percentDailyValue: NEGATIVE_PERCENT,
            },
            {
                nutrientId: USDA_NUTRIENT_IDS.energy,
                name: 'Energy',
                unit: 'kcal',
                amountPer100g: ENERGY_AMOUNT,
                dailyValue: null,
                percentDailyValue: null,
            },
        ];

        expect(buildVitaminMicronutrientViews(nutrients)).toEqual([
            expect.objectContaining({
                nutrientId: USDA_NUTRIENT_IDS.vitaminC,
                percentDailyValueWidth: CLAMPED_PERCENT_WIDTH,
            }),
        ]);
        expect(buildMineralMicronutrientViews(nutrients)).toEqual([
            expect.objectContaining({
                nutrientId: USDA_NUTRIENT_IDS.calcium,
                percentDailyValueWidth: 0,
            }),
        ]);
    });

    it('builds sorted daily key nutrients and skips non-key nutrients', () => {
        const nutrients: DailyMicronutrient[] = [
            {
                nutrientId: USDA_NUTRIENT_IDS.zinc,
                name: 'Zinc',
                unit: 'mg',
                totalAmount: ZINC_TOTAL_AMOUNT,
                dailyValue: null,
                percentDailyValue: ZINC_PERCENT_DAILY_VALUE,
            },
            {
                nutrientId: USDA_NUTRIENT_IDS.energy,
                name: 'Energy',
                unit: 'kcal',
                totalAmount: ENERGY_AMOUNT,
                dailyValue: null,
                percentDailyValue: null,
            },
            {
                nutrientId: USDA_NUTRIENT_IDS.calcium,
                name: 'Calcium',
                unit: 'mg',
                totalAmount: CALCIUM_AMOUNT,
                dailyValue: null,
                percentDailyValue: ABOVE_MAX_PERCENT,
            },
        ];

        expect(buildDailyMicronutrientViews(nutrients)).toEqual([
            expect.objectContaining({
                name: 'Calcium',
                percentDailyValueWidth: CLAMPED_PERCENT_WIDTH,
            }),
            expect.objectContaining({
                name: 'Zinc',
                percentDailyValueWidth: ZINC_PERCENT_DAILY_VALUE,
            }),
        ]);
    });
});
