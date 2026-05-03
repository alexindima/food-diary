import { inject, Injectable } from '@angular/core';
import {
    type AbstractControl,
    type FormArray,
    FormControl,
    FormGroup,
    type ValidationErrors,
    type ValidatorFn,
    Validators,
} from '@angular/forms';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { PremiumRequiredDialogComponent } from '../../../components/shared/premium-required-dialog/premium-required-dialog.component';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import {
    ItemSelectDialogComponent,
    type ItemSelectDialogData,
    type ItemSelection,
} from '../../../shared/dialogs/item-select-dialog/item-select-dialog.component';
import { calculateCalorieMismatchWarning, getControlNumericValue, roundNutrient } from '../../../shared/lib/nutrition-form.utils';
import { type UserAiUsageResponse } from '../../../shared/models/ai.data';
import { type ImageSelection } from '../../../shared/models/image-upload.data';
import { type Product } from '../../products/models/product.data';
import { type Recipe } from '../../recipes/models/recipe.data';
import { MealService } from '../api/meal.service';
import {
    type CalorieMismatchWarning,
    type ConsumptionFormData,
    type ConsumptionItemFormData,
    type ConsumptionItemFormValues,
    type MealNutritionSummaryState,
    type NutritionTotals,
} from '../components/manage/base-meal-manage.types';
import {
    type ConsumptionManageRedirectAction,
    type ConsumptionManageSuccessDialogData,
    MealManageSuccessDialogComponent,
} from '../dialogs/manage-success-dialog/meal-manage-success-dialog.component';
import type { MealPhotoRecognitionDialogComponent } from '../dialogs/photo-recognition-dialog/meal-photo-recognition-dialog.component';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
} from '../models/meal.data';
import { RecipeServingWeightService } from './recipe-serving-weight.service';

@Injectable({ providedIn: 'root' })
export class MealManageFacade {
    private readonly mealService = inject(MealService);
    private readonly aiFoodService = inject(AiFoodService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly recipeWeight = inject(RecipeServingWeightService);

    public async loadAiUsage(): Promise<UserAiUsageResponse | null> {
        return firstValueFrom(this.aiFoodService.getUsageSummary());
    }

    public ensurePremiumAccess(): boolean {
        if (this.authService.isPremium()) {
            return true;
        }

        this.fdDialogService
            .open<PremiumRequiredDialogComponent, never, boolean>(PremiumRequiredDialogComponent, { preset: 'confirm' })
            .afterClosed()
            .subscribe(confirmed => {
                if (confirmed) {
                    void this.navigationService.navigateToPremiumAccess();
                }
            });
        return false;
    }

    public async submitConsumption(consumption: Consumption | null, consumptionData: ConsumptionManageDto): Promise<Consumption | null> {
        return consumption
            ? await firstValueFrom(this.mealService.update(consumption.id, consumptionData))
            : await firstValueFrom(this.mealService.create(consumptionData));
    }

    public async showSuccessRedirect(isEdit: boolean): Promise<void> {
        const data: ConsumptionManageSuccessDialogData = { isEdit };
        const redirectAction = await firstValueFrom(
            this.fdDialogService
                .open<MealManageSuccessDialogComponent, ConsumptionManageSuccessDialogData, ConsumptionManageRedirectAction>(
                    MealManageSuccessDialogComponent,
                    {
                        size: 'sm',
                        data,
                    },
                )
                .afterClosed(),
        );

        if (redirectAction === 'Home') {
            await this.navigationService.navigateToHome();
        } else if (redirectAction === 'ConsumptionList') {
            await this.navigationService.navigateToConsumptionList();
        }
    }

    public async openAiPhotoSessionDialog(): Promise<ConsumptionAiSessionManageDto | null> {
        const { MealPhotoRecognitionDialogComponent } =
            await import('../dialogs/photo-recognition-dialog/meal-photo-recognition-dialog.component');

        return (
            (await firstValueFrom(
                this.fdDialogService
                    .open<MealPhotoRecognitionDialogComponent, never, ConsumptionAiSessionManageDto | null>(
                        MealPhotoRecognitionDialogComponent,
                        {
                            size: 'lg',
                        },
                    )
                    .afterClosed(),
            )) ?? null
        );
    }

    public async openEditAiPhotoSessionDialog(session: ConsumptionAiSessionManageDto): Promise<ConsumptionAiSessionManageDto | null> {
        const { MealPhotoRecognitionDialogComponent } =
            await import('../dialogs/photo-recognition-dialog/meal-photo-recognition-dialog.component');
        const selection: ImageSelection | null = session.imageUrl
            ? { url: session.imageUrl ?? null, assetId: session.imageAssetId ?? null }
            : null;

        return (
            (await firstValueFrom(
                this.fdDialogService
                    .open<
                        MealPhotoRecognitionDialogComponent,
                        { initialSelection: ImageSelection | null; initialSession: ConsumptionAiSessionManageDto | null; mode: 'edit' },
                        ConsumptionAiSessionManageDto | null
                    >(MealPhotoRecognitionDialogComponent, {
                        size: 'lg',
                        data: { initialSelection: selection, initialSession: session, mode: 'edit' },
                    })
                    .afterClosed(),
            )) ?? null
        );
    }

    public addAiSession(
        sessions: ConsumptionAiSessionManageDto[],
        session: ConsumptionAiSessionManageDto,
    ): ConsumptionAiSessionManageDto[] {
        return [...sessions, session];
    }

    public removeAiSession(sessions: ConsumptionAiSessionManageDto[], index: number): ConsumptionAiSessionManageDto[] {
        return sessions.filter((_, currentIndex) => currentIndex !== index);
    }

    public replaceAiSession(
        sessions: ConsumptionAiSessionManageDto[],
        index: number,
        nextSession: ConsumptionAiSessionManageDto,
    ): ConsumptionAiSessionManageDto[] {
        return sessions.map((item, currentIndex) => (currentIndex === index ? nextSession : item));
    }

    public createConsumptionItem(
        product: Product | null = null,
        recipe: Recipe | null = null,
        amount: number | null = null,
        sourceType: ConsumptionSourceType = ConsumptionSourceType.Product,
    ): FormGroup<ConsumptionItemFormData> {
        const group = new FormGroup<ConsumptionItemFormData>({
            sourceType: new FormControl<ConsumptionSourceType>(sourceType, { nonNullable: true }),
            product: new FormControl<Product | null>(product),
            recipe: new FormControl<Recipe | null>(recipe),
            amount: new FormControl<number | null>(amount),
        });

        this.configureItemType(group, sourceType);
        this.updateAmountControlState(group);
        return group;
    }

    public configureItemType(group: FormGroup<ConsumptionItemFormData>, type: ConsumptionSourceType, clearSelection = false): void {
        group.controls.sourceType.setValue(type);

        if (type === ConsumptionSourceType.Product) {
            if (clearSelection) {
                group.controls.recipe.setValue(null);
            }
        } else if (clearSelection) {
            group.controls.product.setValue(null);
        }

        if (clearSelection) {
            group.controls.amount.setValue(null);
            group.controls.amount.markAsUntouched();
        }

        this.updateAmountControlState(group);
    }

    public async openItemSelectionDialog(group: FormGroup<ConsumptionItemFormData>, initialTab: 'Product' | 'Recipe'): Promise<void> {
        const selection = await firstValueFrom(
            this.fdDialogService
                .open<ItemSelectDialogComponent, ItemSelectDialogData, ItemSelection | null>(ItemSelectDialogComponent, {
                    preset: 'list',
                    data: { initialTab },
                })
                .afterClosed(),
        );

        if (!selection) {
            return;
        }

        if (selection.type === 'Product') {
            group.patchValue({
                product: selection.product,
                recipe: null,
            });
            this.configureItemType(group, ConsumptionSourceType.Product);
            return;
        }

        this.recipeWeight.loadServingWeight(selection.recipe).subscribe();
        group.patchValue({
            recipe: selection.recipe,
            product: null,
        });
        this.configureItemType(group, ConsumptionSourceType.Recipe);
    }

    public ensureRecipeWeightForExistingItem(
        group: FormGroup<ConsumptionItemFormData>,
        servingsAmount: number,
        recipe: Recipe | null,
    ): void {
        if (!recipe) {
            return;
        }

        this.recipeWeight.loadServingWeight(recipe).subscribe(servingWeight => {
            if (!servingWeight) {
                return;
            }

            const grams = servingsAmount * servingWeight;
            group.controls.amount.setValue(grams);
        });
    }

    public convertRecipeGramsToServings(recipe: Recipe | null, grams: number): number {
        return this.recipeWeight.convertGramsToServings(recipe, grams);
    }

    public convertRecipeServingsToGrams(recipe: Recipe | null, servings: number): number {
        return this.recipeWeight.convertServingsToGrams(recipe, servings);
    }

    public createItemsValidator(getAiSessions: () => ConsumptionAiSessionManageDto[]): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            const value = control.value as ConsumptionItemFormValues[] | null;
            const hasManualItems = Array.isArray(value) ? value.some(item => Boolean(item.product) || Boolean(item.recipe)) : false;
            const hasAiItems = getAiSessions().length > 0;
            return hasManualItems || hasAiItems ? null : { nonEmptyArray: true };
        };
    }

    public updateItemValidationRules(items: FormArray<FormGroup<ConsumptionItemFormData>>): void {
        items.controls.forEach(group => {
            const isEmpty = this.isItemEmpty(group);
            if (isEmpty) {
                group.controls.product.clearValidators();
                group.controls.recipe.clearValidators();
                group.controls.amount.clearValidators();
            } else {
                const sourceType = group.controls.sourceType.value;
                if (sourceType === ConsumptionSourceType.Product) {
                    group.controls.product.setValidators([Validators.required]);
                    group.controls.recipe.clearValidators();
                } else {
                    group.controls.recipe.setValidators([Validators.required]);
                    group.controls.product.clearValidators();
                }
                group.controls.amount.setValidators([Validators.required, Validators.min(0.01)]);
            }

            group.controls.product.updateValueAndValidity({ emitEvent: false });
            group.controls.recipe.updateValueAndValidity({ emitEvent: false });
            group.controls.amount.updateValueAndValidity({ emitEvent: false });
        });

        items.updateValueAndValidity({ emitEvent: false });
    }

    public updateManualNutritionValidators(form: FormGroup<ConsumptionFormData>, isAuto: boolean): void {
        const caloriesValidators = isAuto ? [Validators.min(0)] : [Validators.required, Validators.min(0)];
        form.controls.manualCalories.setValidators(caloriesValidators);
        form.controls.manualCalories.updateValueAndValidity({ emitEvent: false });

        this.getOptionalManualNutritionControls(form).forEach(control => {
            control.setValidators([Validators.min(0)]);
            control.updateValueAndValidity({ emitEvent: false });
        });
    }

    public buildNutritionSummaryState(
        form: FormGroup<ConsumptionFormData>,
        items: FormArray<FormGroup<ConsumptionItemFormData>>,
        aiSessions: ConsumptionAiSessionManageDto[],
        calorieMismatchThreshold: number,
    ): MealNutritionSummaryState {
        const autoTotals = this.calculateAutoNutritionTotals(items, aiSessions);
        const isAuto = form.controls.isNutritionAutoCalculated.value;
        const summaryTotals = isAuto ? autoTotals : this.getManualNutritionTotals(form);
        return {
            autoTotals: this.roundTotals(autoTotals),
            summaryTotals: this.roundTotals(summaryTotals),
            warning: this.buildCalorieMismatchWarning(form, calorieMismatchThreshold),
        };
    }

    public syncManualNutritionFromTotals(form: FormGroup<ConsumptionFormData>, totals: NutritionTotals): void {
        form.patchValue(
            {
                manualCalories: roundNutrient(totals.calories),
                manualProteins: roundNutrient(totals.proteins),
                manualFats: roundNutrient(totals.fats),
                manualCarbs: roundNutrient(totals.carbs),
                manualFiber: roundNutrient(totals.fiber),
                manualAlcohol: roundNutrient(totals.alcohol),
            },
            { emitEvent: false },
        );
    }

    public getManualNutritionTotals(form: FormGroup<ConsumptionFormData>): NutritionTotals {
        return {
            calories: getControlNumericValue(form.controls.manualCalories),
            proteins: getControlNumericValue(form.controls.manualProteins),
            fats: getControlNumericValue(form.controls.manualFats),
            carbs: getControlNumericValue(form.controls.manualCarbs),
            fiber: getControlNumericValue(form.controls.manualFiber),
            alcohol: getControlNumericValue(form.controls.manualAlcohol),
        };
    }

    private updateAmountControlState(group: FormGroup<ConsumptionItemFormData>): void {
        const shouldDisable = !group.controls.product.value && !group.controls.recipe.value;
        if (shouldDisable && group.controls.amount.enabled) {
            group.controls.amount.disable({ emitEvent: false });
            return;
        }

        if (!shouldDisable && group.controls.amount.disabled) {
            group.controls.amount.enable({ emitEvent: false });
        }
    }

    private calculateAutoNutritionTotals(
        items: FormArray<FormGroup<ConsumptionItemFormData>>,
        aiSessions: ConsumptionAiSessionManageDto[],
    ): NutritionTotals {
        const aiTotals = this.getAiNutritionTotals(aiSessions);
        return items.controls.reduce(
            (totals, group) => {
                const sourceType = group.controls.sourceType.value;
                const amount = group.controls.amount.value || 0;

                if (sourceType === ConsumptionSourceType.Product) {
                    const food = group.controls.product.value;
                    if (!food || food.baseAmount <= 0) {
                        return totals;
                    }

                    const multiplier = amount / food.baseAmount;
                    totals.calories += food.caloriesPerBase * multiplier;
                    totals.proteins += food.proteinsPerBase * multiplier;
                    totals.fats += food.fatsPerBase * multiplier;
                    totals.carbs += food.carbsPerBase * multiplier;
                    totals.fiber += food.fiberPerBase * multiplier;
                    totals.alcohol += food.alcoholPerBase * multiplier;
                    return totals;
                }

                const recipe = group.controls.recipe.value;
                if (recipe && recipe.servings && recipe.servings > 0) {
                    const servings = recipe.servings <= 0 ? 1 : recipe.servings;
                    const caloriesPerServing = (recipe.totalCalories ?? 0) / servings;
                    const proteinsPerServing = (recipe.totalProteins ?? 0) / servings;
                    const fatsPerServing = (recipe.totalFats ?? 0) / servings;
                    const carbsPerServing = (recipe.totalCarbs ?? 0) / servings;
                    const fiberPerServing = (recipe.totalFiber ?? 0) / servings;
                    const alcoholPerServing = (recipe.totalAlcohol ?? 0) / servings;
                    const servingsAmount = this.recipeWeight.convertGramsToServings(recipe, amount);

                    totals.calories += caloriesPerServing * servingsAmount;
                    totals.proteins += proteinsPerServing * servingsAmount;
                    totals.fats += fatsPerServing * servingsAmount;
                    totals.carbs += carbsPerServing * servingsAmount;
                    totals.fiber += fiberPerServing * servingsAmount;
                    totals.alcohol += alcoholPerServing * servingsAmount;
                }

                return totals;
            },
            { ...aiTotals },
        );
    }

    private getAiNutritionTotals(aiSessions: ConsumptionAiSessionManageDto[]): NutritionTotals {
        return aiSessions.reduce(
            (totals, session) =>
                session.items.reduce(
                    (sessionTotals, item) => ({
                        calories: sessionTotals.calories + item.calories,
                        proteins: sessionTotals.proteins + item.proteins,
                        fats: sessionTotals.fats + item.fats,
                        carbs: sessionTotals.carbs + item.carbs,
                        fiber: sessionTotals.fiber + item.fiber,
                        alcohol: sessionTotals.alcohol + item.alcohol,
                    }),
                    totals,
                ),
            { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 },
        );
    }

    private buildCalorieMismatchWarning(
        form: FormGroup<ConsumptionFormData>,
        calorieMismatchThreshold: number,
    ): CalorieMismatchWarning | null {
        if (form.controls.isNutritionAutoCalculated.value) {
            return null;
        }

        const calories = getControlNumericValue(form.controls.manualCalories);
        const proteins = getControlNumericValue(form.controls.manualProteins);
        const fats = getControlNumericValue(form.controls.manualFats);
        const carbs = getControlNumericValue(form.controls.manualCarbs);
        const alcohol = getControlNumericValue(form.controls.manualAlcohol);
        return calculateCalorieMismatchWarning(calories, proteins, fats, carbs, alcohol, calorieMismatchThreshold);
    }

    private roundTotals(totals: NutritionTotals): NutritionTotals {
        return {
            calories: roundNutrient(totals.calories),
            proteins: roundNutrient(totals.proteins),
            fats: roundNutrient(totals.fats),
            carbs: roundNutrient(totals.carbs),
            fiber: roundNutrient(totals.fiber),
            alcohol: roundNutrient(totals.alcohol),
        };
    }

    private isItemEmpty(group: FormGroup<ConsumptionItemFormData>): boolean {
        const hasSource = Boolean(group.controls.product.value) || Boolean(group.controls.recipe.value);
        const amount = group.controls.amount.value ?? 0;
        return !hasSource && amount <= 0;
    }

    private getOptionalManualNutritionControls(form: FormGroup<ConsumptionFormData>): Array<FormControl<number | null>> {
        return [
            form.controls.manualProteins,
            form.controls.manualFats,
            form.controls.manualCarbs,
            form.controls.manualFiber,
            form.controls.manualAlcohol,
        ];
    }
}
