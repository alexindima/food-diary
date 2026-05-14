import { signal } from '@angular/core';
import { type FormControl, type FormGroup, Validators } from '@angular/forms';

import { checkMacrosError } from '../../../../../shared/lib/nutrition-form.utils';
import type { NutrientData } from '../../../../../shared/models/charts.data';
import type { RecipeNutritionSummary } from '../../../lib/recipe-manage.facade';
import type { Recipe } from '../../../models/recipe.data';
import type { NutritionMode, NutritionScaleMode, RecipeFormData, RecipeFormValues } from './recipe-manage.types';

export type RecipeNutritionFormOperations = {
    calculateAutoSummary: (steps: FormGroup<RecipeFormData>['controls']['steps']) => RecipeNutritionSummary;
    fromRecipeTotal: (value: number | null | undefined, scaleMode: NutritionScaleMode, servings: number) => number;
    getSummaryFromRecipe: (recipeData: Recipe | null, fallback: RecipeNutritionSummary) => RecipeNutritionSummary;
    roundNutritionValue: (value: number) => number;
    toRecipeTotal: (value: number | null | undefined, scaleMode: NutritionScaleMode, servings: number) => number;
};

export class RecipeNutritionFormManager {
    public readonly totalCalories = signal<number>(0);
    public readonly totalFiber = signal<number>(0);
    public readonly totalAlcohol = signal<number>(0);
    public readonly nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public readonly nutritionMode = signal<NutritionMode>('auto');

    public nutritionScaleMode: NutritionScaleMode = 'recipe';

    public constructor(
        private readonly form: FormGroup<RecipeFormData>,
        private readonly operations: RecipeNutritionFormOperations,
    ) {}

    public initialize(): void {
        this.nutritionMode.set(this.form.controls.calculateNutritionAutomatically.value ? 'auto' : 'manual');
        this.recalculateNutrientsFromForm();
        this.updateManualNutritionValidators(this.form.controls.calculateNutritionAutomatically.value);
    }

    public handleAutoCalculationChange(isAuto: boolean): void {
        this.nutritionMode.set(isAuto ? 'auto' : 'manual');
        if (!isAuto) {
            this.patchManualNutritionFromCurrentSummary();
        }
        this.updateManualNutritionValidators(isAuto);
        this.updateSummaryFromForm();
    }

    public onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'manual' ? 'manual' : 'auto';
        if (this.nutritionMode() === resolvedMode) {
            return;
        }

        this.nutritionMode.set(resolvedMode);
        this.form.controls.calculateNutritionAutomatically.setValue(resolvedMode === 'auto');
    }

    public onNutritionScaleModeChange(nextMode: string): void {
        const resolvedMode: NutritionScaleMode = nextMode === 'portion' ? 'portion' : 'recipe';
        if (this.nutritionScaleMode === resolvedMode) {
            return;
        }

        const servings = this.getServingsValue();
        const factor = resolvedMode === 'portion' ? 1 / servings : servings;
        this.convertManualNutritionControls(factor);
        this.nutritionScaleMode = resolvedMode;
        this.updateSummaryFromForm();
    }

    public hasMacrosError(): boolean {
        if (this.form.controls.calculateNutritionAutomatically.value) {
            return false;
        }

        return checkMacrosError([
            this.form.controls.manualProteins,
            this.form.controls.manualFats,
            this.form.controls.manualCarbs,
            this.form.controls.manualAlcohol,
        ]);
    }

    public updateNutrientSummary(recipeData: Recipe | null): void {
        const summary = this.operations.getSummaryFromRecipe(recipeData, {
            calories: this.totalCalories(),
            proteins: this.nutrientChartData().proteins,
            fats: this.nutrientChartData().fats,
            carbs: this.nutrientChartData().carbs,
            fiber: this.totalFiber(),
            alcohol: this.totalAlcohol(),
        });

        this.setNutrientSummary(summary);
    }

    public updateSummaryFromForm(): void {
        if (this.form.controls.calculateNutritionAutomatically.value) {
            this.recalculateNutrientsFromForm();
            return;
        }

        this.setNutrientSummary({
            calories: this.toRecipeTotal(this.form.controls.manualCalories.value),
            proteins: this.toRecipeTotal(this.form.controls.manualProteins.value),
            fats: this.toRecipeTotal(this.form.controls.manualFats.value),
            carbs: this.toRecipeTotal(this.form.controls.manualCarbs.value),
            fiber: this.toRecipeTotal(this.form.controls.manualFiber.value),
            alcohol: this.toRecipeTotal(this.form.controls.manualAlcohol.value),
        });
    }

    public recalculateNutrientsFromForm(): void {
        const summary = this.operations.calculateAutoSummary(this.form.controls.steps);
        this.setNutrientSummary(summary);
    }

    public getServingsValue(): number {
        const servings = Number(this.form.controls.servings.value);
        return Number.isFinite(servings) && servings > 0 ? servings : 1;
    }

    public toRecipeTotal(value: number | null | undefined): number {
        return this.operations.toRecipeTotal(value, this.nutritionScaleMode, this.getServingsValue());
    }

    private patchManualNutritionFromCurrentSummary(): void {
        this.form.patchValue(
            {
                manualCalories: this.fromRecipeTotal(this.totalCalories()),
                manualProteins: this.fromRecipeTotal(this.nutrientChartData().proteins),
                manualFats: this.fromRecipeTotal(this.nutrientChartData().fats),
                manualCarbs: this.fromRecipeTotal(this.nutrientChartData().carbs),
                manualFiber: this.fromRecipeTotal(this.totalFiber()),
                manualAlcohol: this.fromRecipeTotal(this.totalAlcohol()),
            },
            { emitEvent: false },
        );
    }

    private setNutrientSummary({ calories, proteins, fats, carbs, fiber, alcohol }: RecipeNutritionSummary): void {
        this.totalCalories.set(this.operations.roundNutritionValue(calories));
        this.totalFiber.set(this.operations.roundNutritionValue(fiber));
        this.totalAlcohol.set(this.operations.roundNutritionValue(alcohol));
        this.nutrientChartData.set({
            proteins: this.operations.roundNutritionValue(proteins),
            fats: this.operations.roundNutritionValue(fats),
            carbs: this.operations.roundNutritionValue(carbs),
        });

        if (this.form.controls.calculateNutritionAutomatically.value) {
            this.patchManualNutritionFromCurrentSummary();
        }
    }

    private updateManualNutritionValidators(isAuto: boolean): void {
        const caloriesValidators = isAuto ? [Validators.min(0)] : [Validators.required, Validators.min(0)];
        this.form.controls.manualCalories.setValidators(caloriesValidators);
        this.form.controls.manualCalories.updateValueAndValidity({ emitEvent: false });

        this.getOptionalManualNutritionControls().forEach(control => {
            control.setValidators([Validators.min(0)]);
            control.updateValueAndValidity({ emitEvent: false });
        });
    }

    private fromRecipeTotal(value: number | null | undefined): number {
        return this.operations.fromRecipeTotal(value, this.nutritionScaleMode, this.getServingsValue());
    }

    private convertManualNutritionControls(factor: number): void {
        const fields: Array<
            keyof Pick<
                RecipeFormValues,
                'manualCalories' | 'manualProteins' | 'manualFats' | 'manualCarbs' | 'manualFiber' | 'manualAlcohol'
            >
        > = ['manualCalories', 'manualProteins', 'manualFats', 'manualCarbs', 'manualFiber', 'manualAlcohol'];
        const patch: Partial<RecipeFormValues> = {};

        fields.forEach(field => {
            const raw = Number(this.form.controls[field].value);
            if (!Number.isFinite(raw)) {
                return;
            }
            patch[field] = this.operations.roundNutritionValue(raw * factor);
        });

        this.form.patchValue(patch, { emitEvent: false });
    }

    private getOptionalManualNutritionControls(): Array<FormControl<number | null>> {
        return [
            this.form.controls.manualProteins,
            this.form.controls.manualFats,
            this.form.controls.manualCarbs,
            this.form.controls.manualFiber,
            this.form.controls.manualAlcohol,
        ];
    }
}
