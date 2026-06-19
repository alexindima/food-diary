import { inject, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';

import { PremiumRequiredDialogComponent } from '../../../../components/shared/premium-required-dialog/premium-required-dialog';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { ItemSelectDialogComponent } from '../../../../shared/dialogs/item-select-dialog/item-select-dialog';
import type {
    ItemSelectDialogData,
    ItemSelection,
} from '../../../../shared/dialogs/item-select-dialog/item-select-dialog-lib/item-select-dialog.types';
import { calculateCalorieMismatchWarning, roundNutrient } from '../../../../shared/lib/nutrition-form.utils';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import type { Product } from '../../../products/models/product.data';
import type { Recipe } from '../../../recipes/models/recipe.data';
import { MealService } from '../../api/meal.service';
import type {
    CalorieMismatchWarning,
    ConsumptionFormValues,
    ConsumptionItemFormValues,
    MealNutritionSummaryState,
    NutritionTotals,
} from '../../components/manage/meal-manage-lib/meal-manage.types';
import { createConsumptionItemValue } from '../../components/manage/meal-manage-lib/meal-manage-form.mapper';
import type { MealPhotoRecognitionDialogComponent } from '../../dialogs/photo-recognition-dialog/meal-photo-recognition-dialog';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../models/meal.data';
import { RecipeServingWeightService } from '../recipe-serving/recipe-serving-weight.service';
import { MEAL_MANAGE_DEFAULT_ITEM_AMOUNT } from './meal-manage.config';

@Service()
export class MealManageFacade {
    private readonly mealService = inject(MealService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly recipeWeight = inject(RecipeServingWeightService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);

    public ensurePremiumAccess(): boolean {
        if (this.authService.isPremium()) {
            return true;
        }

        this.fdDialogService
            .open<PremiumRequiredDialogComponent, never, boolean>(PremiumRequiredDialogComponent, { preset: 'confirm' })
            .afterClosed()
            .subscribe(confirmed => {
                if (confirmed === true) {
                    void this.navigationService.navigateToPremiumAccessAsync();
                }
            });
        return false;
    }

    public async submitConsumptionAsync(consumption: Consumption | null, consumptionData: ConsumptionManageDto): Promise<Consumption> {
        return consumption !== null
            ? firstValueFrom(this.mealService.update(consumption.id, consumptionData))
            : firstValueFrom(this.mealService.create(consumptionData));
    }

    public async showSuccessToastAndRedirectAsync(isEdit: boolean): Promise<void> {
        this.toastService.success(
            this.translateService.instant(isEdit ? 'CONSUMPTION_MANAGE.EDIT_SUCCESS' : 'CONSUMPTION_MANAGE.CREATE_SUCCESS'),
        );
        await this.navigationService.navigateToConsumptionListAsync();
    }

    public async openEditAiPhotoSessionDialogAsync(session: ConsumptionAiSessionManageDto): Promise<ConsumptionAiSessionManageDto | null> {
        const { MealPhotoRecognitionDialogComponent } =
            await import('../../dialogs/photo-recognition-dialog/meal-photo-recognition-dialog');
        const selection: ImageSelection | null =
            session.imageUrl !== null && session.imageUrl !== undefined && session.imageUrl.length > 0
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
    ): ConsumptionItemFormValues {
        return createConsumptionItemValue(product, recipe, amount, sourceType);
    }

    public createConsumptionItemValue(
        product: Product | null = null,
        recipe: Recipe | null = null,
        amount: number | null = null,
        sourceType: ConsumptionSourceType = ConsumptionSourceType.Product,
    ): ConsumptionItemFormValues {
        return {
            sourceType,
            product,
            recipe,
            amount,
        };
    }

    public configureItemType(
        item: ConsumptionItemFormValues,
        type: ConsumptionSourceType,
        clearSelection = false,
    ): ConsumptionItemFormValues {
        return {
            ...item,
            sourceType: type,
            product: clearSelection && type === ConsumptionSourceType.Recipe ? null : item.product,
            recipe: clearSelection && type === ConsumptionSourceType.Product ? null : item.recipe,
            amount: clearSelection ? null : item.amount,
        };
    }

    public async openItemSelectionDialogAsync(initialTab: 'Product' | 'Recipe'): Promise<ConsumptionItemFormValues | null> {
        const selection = await firstValueFrom(
            this.fdDialogService
                .open<ItemSelectDialogComponent, ItemSelectDialogData, ItemSelection | null>(ItemSelectDialogComponent, {
                    preset: 'list',
                    data: { initialTab },
                })
                .afterClosed(),
        );

        if (selection === null || selection === undefined) {
            return null;
        }

        if (selection.type === 'Product') {
            return createConsumptionItemValue(
                selection.product,
                null,
                this.resolveProductAmount(selection.product),
                ConsumptionSourceType.Product,
            );
        }

        const amount = await this.resolveRecipeServingsToGramsAsync(selection.recipe, MEAL_MANAGE_DEFAULT_ITEM_AMOUNT);
        return createConsumptionItemValue(null, selection.recipe, amount, ConsumptionSourceType.Recipe);
    }

    public async resolveRecipeServingsToGramsAsync(recipe: Recipe | null, servingsAmount: number): Promise<number> {
        const servingWeight = await firstValueFrom(this.recipeWeight.loadServingWeight(recipe));
        return servingWeight !== null && servingWeight > 0 ? servingsAmount * servingWeight : servingsAmount;
    }

    public convertRecipeGramsToServings(recipe: Recipe | null, grams: number): number {
        return this.recipeWeight.convertGramsToServings(recipe, grams);
    }

    public convertRecipeServingsToGrams(recipe: Recipe | null, servings: number): number {
        return this.recipeWeight.convertServingsToGrams(recipe, servings);
    }

    public buildNutritionSummaryStateFromValues(
        formValue: ConsumptionFormValues,
        aiSessions: ConsumptionAiSessionManageDto[],
        calorieMismatchThreshold: number,
    ): MealNutritionSummaryState {
        const autoTotals = this.calculateAutoNutritionTotalsFromValues(formValue.items, aiSessions);
        const summaryTotals = formValue.isNutritionAutoCalculated ? autoTotals : this.getManualNutritionTotalsFromValue(formValue);
        return {
            autoTotals: this.roundTotals(autoTotals),
            summaryTotals: this.roundTotals(summaryTotals),
            warning: this.buildCalorieMismatchWarningFromValue(formValue, calorieMismatchThreshold),
        };
    }

    public buildManualNutritionPatchFromTotals(totals: NutritionTotals): Partial<ConsumptionFormValues> {
        return {
            manualCalories: roundNutrient(totals.calories),
            manualProteins: roundNutrient(totals.proteins),
            manualFats: roundNutrient(totals.fats),
            manualCarbs: roundNutrient(totals.carbs),
            manualFiber: roundNutrient(totals.fiber),
            manualAlcohol: roundNutrient(totals.alcohol),
        };
    }

    public getManualNutritionTotalsFromValue(formValue: ConsumptionFormValues): NutritionTotals {
        return {
            calories: this.getNumericValue(formValue.manualCalories),
            proteins: this.getNumericValue(formValue.manualProteins),
            fats: this.getNumericValue(formValue.manualFats),
            carbs: this.getNumericValue(formValue.manualCarbs),
            fiber: this.getNumericValue(formValue.manualFiber),
            alcohol: this.getNumericValue(formValue.manualAlcohol),
        };
    }

    private resolveProductAmount(product: Product): number {
        if (product.defaultPortionAmount > 0) {
            return product.defaultPortionAmount;
        }

        if (product.baseAmount > 0) {
            return product.baseAmount;
        }

        return MEAL_MANAGE_DEFAULT_ITEM_AMOUNT;
    }

    private calculateAutoNutritionTotalsFromValues(
        items: ConsumptionItemFormValues[],
        aiSessions: ConsumptionAiSessionManageDto[],
    ): NutritionTotals {
        const aiTotals = this.getAiNutritionTotals(aiSessions);
        return items.reduce((totals, item) => this.addItemNutritionTotalsFromValue(totals, item), { ...aiTotals });
    }

    private addItemNutritionTotalsFromValue(totals: NutritionTotals, item: ConsumptionItemFormValues): NutritionTotals {
        const amount = item.amount ?? 0;

        if (item.sourceType === ConsumptionSourceType.Product) {
            return this.addProductNutritionTotals(totals, item.product, amount);
        }

        return this.addRecipeNutritionTotals(totals, item.recipe, amount);
    }

    private addProductNutritionTotals(totals: NutritionTotals, food: Product | null, amount: number): NutritionTotals {
        if (food === null || food.baseAmount <= 0) {
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

    private addRecipeNutritionTotals(totals: NutritionTotals, recipe: Recipe | null, amount: number): NutritionTotals {
        if (recipe === null || recipe.servings <= 0) {
            return totals;
        }

        const caloriesPerServing = (recipe.totalCalories ?? 0) / recipe.servings;
        const proteinsPerServing = (recipe.totalProteins ?? 0) / recipe.servings;
        const fatsPerServing = (recipe.totalFats ?? 0) / recipe.servings;
        const carbsPerServing = (recipe.totalCarbs ?? 0) / recipe.servings;
        const fiberPerServing = (recipe.totalFiber ?? 0) / recipe.servings;
        const alcoholPerServing = (recipe.totalAlcohol ?? 0) / recipe.servings;
        const servingsAmount = this.recipeWeight.convertGramsToServings(recipe, amount);

        totals.calories += caloriesPerServing * servingsAmount;
        totals.proteins += proteinsPerServing * servingsAmount;
        totals.fats += fatsPerServing * servingsAmount;
        totals.carbs += carbsPerServing * servingsAmount;
        totals.fiber += fiberPerServing * servingsAmount;
        totals.alcohol += alcoholPerServing * servingsAmount;
        return totals;
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

    private buildCalorieMismatchWarningFromValue(
        formValue: ConsumptionFormValues,
        calorieMismatchThreshold: number,
    ): CalorieMismatchWarning | null {
        if (formValue.isNutritionAutoCalculated) {
            return null;
        }

        return calculateCalorieMismatchWarning({
            calories: this.getNumericValue(formValue.manualCalories),
            proteins: this.getNumericValue(formValue.manualProteins),
            fats: this.getNumericValue(formValue.manualFats),
            carbs: this.getNumericValue(formValue.manualCarbs),
            alcohol: this.getNumericValue(formValue.manualAlcohol),
            threshold: calorieMismatchThreshold,
        });
    }

    private getNumericValue(value: number | string | null | undefined): number {
        if (value === null || value === undefined || value === '') {
            return 0;
        }

        const numericValue = typeof value === 'string' ? Number(value.replace(',', '.').replaceAll(/[^\d.-]/g, '')) : Number(value);
        return Number.isFinite(numericValue) ? Math.max(0, numericValue) : 0;
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
}
