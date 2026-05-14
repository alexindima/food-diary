import { inject, Injectable, signal } from '@angular/core';
import type { FormArray, FormGroup } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { finalize, map, type Observable } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import {
    ItemSelectDialogComponent,
    type ItemSelectDialogData,
    type ItemSelection,
} from '../../../shared/dialogs/item-select-dialog/item-select-dialog.component';
import { NUTRIENT_ROUNDING_FACTOR } from '../../../shared/lib/nutrition.constants';
import { RecipeService } from '../api/recipe.service';
import type { IngredientFormData, NutritionScaleMode, StepFormData } from '../components/manage/recipe-manage-lib/recipe-manage.types';
import type { Recipe, RecipeDto } from '../models/recipe.data';

export type RecipeNutritionSummary = {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
};

@Injectable({ providedIn: 'root' })
export class RecipeManageFacade {
    private readonly recipeService = inject(RecipeService);
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly dialogRef = inject(FdUiDialogRef<unknown, Recipe | null>, { optional: true });

    public readonly globalError = signal<string | null>(null);
    public readonly isSubmitting = signal(false);

    public openItemSelectionDialog(): Observable<ItemSelection | null> {
        return this.dialogService
            .open<ItemSelectDialogComponent, ItemSelectDialogData, ItemSelection | null>(ItemSelectDialogComponent, {
                size: 'lg',
                data: { initialTab: 'Product' },
            })
            .afterClosed()
            .pipe(map(selection => selection ?? null));
    }

    public applyItemSelection(foodGroup: FormGroup<IngredientFormData>, selection: ItemSelection): void {
        if (selection.type === 'Product') {
            const food = selection.product;
            const defaultAmount = food.defaultPortionAmount;
            foodGroup.patchValue({
                food,
                foodName: food.name,
                nestedRecipeId: null,
                nestedRecipeName: null,
                amount: defaultAmount,
            });
            return;
        }

        const recipe = selection.recipe;
        foodGroup.patchValue({
            food: null,
            foodName: recipe.name,
            nestedRecipeId: recipe.id,
            nestedRecipeName: recipe.name,
            amount: 1,
        });
    }

    public getSummaryFromRecipe(recipeData: Recipe | null, fallback: RecipeNutritionSummary): RecipeNutritionSummary {
        if (recipeData === null) {
            return {
                calories: 0,
                proteins: 0,
                fats: 0,
                carbs: 0,
                fiber: 0,
                alcohol: 0,
            };
        }

        return {
            calories: recipeData.totalCalories ?? fallback.calories,
            proteins: recipeData.totalProteins ?? fallback.proteins,
            fats: recipeData.totalFats ?? fallback.fats,
            carbs: recipeData.totalCarbs ?? fallback.carbs,
            fiber: recipeData.totalFiber ?? fallback.fiber,
            alcohol: recipeData.totalAlcohol ?? fallback.alcohol,
        };
    }

    public calculateAutoSummary(stepsArray: FormArray<FormGroup<StepFormData>>): RecipeNutritionSummary {
        if (stepsArray.length === 0) {
            return {
                calories: 0,
                proteins: 0,
                fats: 0,
                carbs: 0,
                fiber: 0,
                alcohol: 0,
            };
        }

        let calories = 0;
        let proteins = 0;
        let fats = 0;
        let carbs = 0;
        let fiber = 0;
        let alcohol = 0;

        stepsArray.controls.forEach(stepGroup => {
            stepGroup.controls.ingredients.controls.forEach(ingredientGroup => {
                const food = ingredientGroup.controls.food.value;
                const amount = ingredientGroup.controls.amount.value;

                if (food === null || amount === null || amount <= 0) {
                    return;
                }

                const baseAmount = food.baseAmount > 0 ? food.baseAmount : 1;
                const multiplier = amount / baseAmount;

                calories += food.caloriesPerBase * multiplier;
                proteins += food.proteinsPerBase * multiplier;
                fats += food.fatsPerBase * multiplier;
                carbs += food.carbsPerBase * multiplier;
                fiber += food.fiberPerBase * multiplier;
                alcohol += food.alcoholPerBase * multiplier;
            });
        });

        return {
            calories: this.roundNutrient(calories),
            proteins: this.roundNutrient(proteins),
            fats: this.roundNutrient(fats),
            carbs: this.roundNutrient(carbs),
            fiber: this.roundNutrient(fiber),
            alcohol: this.roundNutrient(alcohol),
        };
    }

    public fromRecipeTotal(value: number | null | undefined, scaleMode: NutritionScaleMode, servings: number): number {
        const normalized = Number(value ?? 0);
        if (!Number.isFinite(normalized)) {
            return 0;
        }

        if (scaleMode === 'recipe') {
            return normalized;
        }

        return this.roundNutrient(normalized / this.normalizeServings(servings));
    }

    public toRecipeTotal(value: number | null | undefined, scaleMode: NutritionScaleMode, servings: number): number {
        const normalized = Number(value ?? 0);
        if (!Number.isFinite(normalized)) {
            return 0;
        }

        if (scaleMode === 'recipe') {
            return normalized;
        }

        return this.roundNutrient(normalized * this.normalizeServings(servings));
    }

    public roundNutritionValue(value: number): number {
        return this.roundNutrient(value);
    }

    public addRecipe(recipeData: RecipeDto): void {
        this.isSubmitting.set(true);
        this.recipeService
            .create(recipeData)
            .pipe(
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
            )
            .subscribe({
                next: recipe => void this.handleSubmitResponseAsync(recipe),
                error: (error: unknown) => {
                    this.handleSubmitError(error);
                },
            });
    }

    public updateRecipe(id: string, recipeData: RecipeDto): void {
        this.isSubmitting.set(true);
        this.recipeService
            .update(id, recipeData)
            .pipe(
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
            )
            .subscribe({
                next: recipe => void this.handleSubmitResponseAsync(recipe),
                error: (error: unknown) => {
                    this.handleSubmitError(error);
                },
            });
    }

    public async cancelManageAsync(): Promise<void> {
        if (this.dialogRef !== null) {
            this.dialogRef.close(null);
            return;
        }

        await this.navigationService.navigateToRecipeListAsync();
    }

    public clearGlobalError(): void {
        this.globalError.set(null);
    }

    public setGlobalError(message: string, translate = true): void {
        this.globalError.set(translate ? this.translateService.instant(message) : message);
    }

    private async handleSubmitResponseAsync(response: Recipe): Promise<void> {
        this.clearGlobalError();
        if (this.dialogRef !== null) {
            this.dialogRef.close(response);
            return;
        }

        await this.navigationService.navigateToRecipeListAsync();
    }

    private handleSubmitError(error?: unknown): void {
        const message = this.getErrorMessage(error) ?? this.translateService.instant('FORM_ERRORS.UNKNOWN');
        this.setGlobalError(message, false);
    }

    private getErrorMessage(error: unknown): string | null {
        if (!this.isRecord(error)) {
            return null;
        }

        const responseBody = error['error'];
        return this.isRecord(responseBody) && typeof responseBody['message'] === 'string' ? responseBody['message'] : null;
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null && !Array.isArray(value);
    }

    private roundNutrient(value: number): number {
        return Math.round(value * NUTRIENT_ROUNDING_FACTOR) / NUTRIENT_ROUNDING_FACTOR;
    }

    private normalizeServings(servings: number): number {
        return Number.isFinite(servings) && servings > 0 ? servings : 1;
    }
}
