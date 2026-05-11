import { Injectable } from '@angular/core';

const CALORIES_PER_PROTEIN_GRAM = 4;
const CALORIES_PER_FAT_GRAM = 9;
const CALORIES_PER_CARB_GRAM = 4;
const CALORIES_PER_ALCOHOL_GRAM = 7;

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
            proteinValue * CALORIES_PER_PROTEIN_GRAM +
            fatValue * CALORIES_PER_FAT_GRAM +
            carbValue * CALORIES_PER_CARB_GRAM +
            alcoholValue * CALORIES_PER_ALCOHOL_GRAM
        );
    }

    private normalizeMacroValue(value: number | null | undefined): number {
        if (typeof value !== 'number' || !Number.isFinite(value)) {
            return 0;
        }
        return Math.max(0, value);
    }
}
