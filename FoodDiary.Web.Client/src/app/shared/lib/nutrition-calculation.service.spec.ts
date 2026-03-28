import { TestBed } from '@angular/core/testing';
import { NutritionCalculationService } from './nutrition-calculation.service';

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
        const result = service.calculateCaloriesFromMacros(10, 5, 20);
        expect(result).toBe(165);
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
        const result = service.calculateCaloriesFromMacros(-10, -5, -20, -3);
        expect(result).toBe(0);
    });

    it('should include alcohol calories at 7 per gram', () => {
        // 0*4 + 0*9 + 0*4 + 10*7 = 70
        const result = service.calculateCaloriesFromMacros(0, 0, 0, 10);
        expect(result).toBe(70);
    });

    it('should calculate mixed macros with alcohol', () => {
        // 20*4 + 10*9 + 30*4 + 5*7 = 80 + 90 + 120 + 35 = 325
        const result = service.calculateCaloriesFromMacros(20, 10, 30, 5);
        expect(result).toBe(325);
    });
});
