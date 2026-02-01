import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class NutritionCalculationService {
    public calculateCaloriesFromMacros(
        proteins: number | null | undefined,
        fats: number | null | undefined,
        carbs: number | null | undefined,
        alcohol: number | null | undefined = 0,
    ): number {
        const proteinValue = this.normalizeMacroValue(proteins);
        const fatValue = this.normalizeMacroValue(fats);
        const carbValue = this.normalizeMacroValue(carbs);
        const alcoholValue = this.normalizeMacroValue(alcohol);

        return proteinValue * 4 + fatValue * 9 + carbValue * 4 + alcoholValue * 7;
    }

    private normalizeMacroValue(value: number | null | undefined): number {
        if (typeof value !== 'number' || !Number.isFinite(value)) {
            return 0;
        }
        return Math.max(0, value);
    }
}
