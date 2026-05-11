import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { NutritionCalculationService } from './nutrition-calculation.service';

const PROTEIN_GRAMS = 10;
const FAT_GRAMS = 5;
const CARB_GRAMS = 20;
const NEGATIVE_PROTEIN_GRAMS = -10;
const NEGATIVE_FAT_GRAMS = -5;
const NEGATIVE_CARB_GRAMS = -20;
const NEGATIVE_ALCOHOL_GRAMS = -3;
const ALCOHOL_GRAMS = 10;
const MIXED_PROTEIN_GRAMS = 20;
const MIXED_FAT_GRAMS = 10;
const MIXED_CARB_GRAMS = 30;
const MIXED_ALCOHOL_GRAMS = 5;
const EXPECTED_MACRO_CALORIES = 165;
const EXPECTED_ALCOHOL_CALORIES = 70;
const EXPECTED_MIXED_CALORIES = 325;

describe('NutritionCalculationService', () => {
    let service: NutritionCalculationService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [NutritionCalculationService],
        });

        service = TestBed.inject(NutritionCalculationService);
    });

    it('should calculate calories correctly', () => {
        // 10*4 + 5*9 + 20*4 + 0*7 = 40 + 45 + 80 = 165
        const result = service.calculateCaloriesFromMacros(PROTEIN_GRAMS, FAT_GRAMS, CARB_GRAMS);
        expect(result).toBe(EXPECTED_MACRO_CALORIES);
    });

    it('should handle null values as 0', () => {
        const result = service.calculateCaloriesFromMacros(null, null, null, null);
        expect(result).toBe(0);
    });

    it('should handle undefined values as 0', () => {
        const result = service.calculateCaloriesFromMacros(undefined, undefined, undefined, undefined);
        expect(result).toBe(0);
    });

    it('should handle NaN values as 0', () => {
        const result = service.calculateCaloriesFromMacros(NaN, NaN, NaN, NaN);
        expect(result).toBe(0);
    });

    it('should handle Infinity as 0', () => {
        const result = service.calculateCaloriesFromMacros(Infinity, -Infinity, Infinity, -Infinity);
        expect(result).toBe(0);
    });

    it('should handle negative values as 0', () => {
        const result = service.calculateCaloriesFromMacros(
            NEGATIVE_PROTEIN_GRAMS,
            NEGATIVE_FAT_GRAMS,
            NEGATIVE_CARB_GRAMS,
            NEGATIVE_ALCOHOL_GRAMS,
        );
        expect(result).toBe(0);
    });

    it('should include alcohol calories at 7 per gram', () => {
        // 0*4 + 0*9 + 0*4 + 10*7 = 70
        const result = service.calculateCaloriesFromMacros(0, 0, 0, ALCOHOL_GRAMS);
        expect(result).toBe(EXPECTED_ALCOHOL_CALORIES);
    });

    it('should calculate mixed macros with alcohol', () => {
        // 20*4 + 10*9 + 30*4 + 5*7 = 80 + 90 + 120 + 35 = 325
        const result = service.calculateCaloriesFromMacros(MIXED_PROTEIN_GRAMS, MIXED_FAT_GRAMS, MIXED_CARB_GRAMS, MIXED_ALCOHOL_GRAMS);
        expect(result).toBe(EXPECTED_MIXED_CALORIES);
    });
});
