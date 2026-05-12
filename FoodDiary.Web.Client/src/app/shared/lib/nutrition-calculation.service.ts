import { Injectable } from '@angular/core';

import { ALCOHOL_CALORIES_PER_GRAM, CARB_CALORIES_PER_GRAM, FAT_CALORIES_PER_GRAM, PROTEIN_CALORIES_PER_GRAM } from './nutrition.constants';

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

        return (
            proteinValue * PROTEIN_CALORIES_PER_GRAM +
            fatValue * FAT_CALORIES_PER_GRAM +
            carbValue * CARB_CALORIES_PER_GRAM +
            alcoholValue * ALCOHOL_CALORIES_PER_GRAM
        );
    }

    private normalizeMacroValue(value: number | null | undefined): number {
        if (typeof value !== 'number' || !Number.isFinite(value)) {
            return 0;
        }
        return Math.max(0, value);
    }
}
