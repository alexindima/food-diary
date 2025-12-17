import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class NutritionCalculationService {
    public calculateCaloriesFromMacros(
        proteins: number | null | undefined,
        fats: number | null | undefined,
        carbs: number | null | undefined,
    ): number {
        const proteinValue = this.normalizeMacroValue(proteins);
        const fatValue = this.normalizeMacroValue(fats);
        const carbValue = this.normalizeMacroValue(carbs);

        return proteinValue * 4 + fatValue * 9 + carbValue * 4;
    }

    private normalizeMacroValue(value: number | null | undefined): number {
        if (typeof value !== 'number' || !Number.isFinite(value)) {
            return 0;
        }
        return Math.max(0, value);
    }
}
