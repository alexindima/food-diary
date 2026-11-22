import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { MealSourceType } from '../../../../../shared/models/meal.data';
import { recipeServingsFromInput, type ServingMassResult } from '../../../lib/recipe-serving/recipe-display-amount';
import type { MealItemFormValues, NutritionTotals } from '../meal-manage-lib/meal-manage.types';
import {
    formatMealManageAmount,
    formatMealManageMacro,
    formatMealManageNumber,
    getEmptyNutritionTotals,
} from '../meal-manage-lib/meal-manage-view.utils';

@Component({
    selector: 'fd-meal-items-list',
    templateUrl: './meal-items-list.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiHintDirective, FdUiCardComponent, FdUiButtonComponent, FdUiFormErrorComponent, FdUiIconComponent],
})
export class MealItemsListComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly items = input.required<readonly MealItemsListItemState[]>();
    public readonly hasExternalItems = input.required<boolean>();
    public readonly arrayError = input<string | null>(null);
    public readonly renderVersion = input<number>(0);

    public readonly editItem = output<number>();
    public readonly removeItemEvent = output<MealItemFormValues>();
    public readonly openItemSelect = output<number>();
    protected readonly manualItemRows = computed<ManualItemRowViewModel[]>(() => {
        this.renderVersion();
        this.activeLang();

        return this.items()
            .map(({ formItem }, index) => ({ index, item: formItem }))
            .filter(({ item }) => item.product !== null || item.recipe !== null)
            .map(({ index, item }) => {
                const totals = this.getManualItemTotals(item);

                return {
                    index,
                    formItem: item,
                    imageUrl: this.getManualItemImageUrl(item),
                    icon: item.sourceType === MealSourceType.Recipe ? 'menu_book' : 'restaurant',
                    sourceName: this.getItemSourceName(item),
                    amountLabel: this.formatManualAmount(item),
                    caloriesLabel: formatMealManageMacro(totals.calories, 'GENERAL.UNITS.KCAL', this.translateService),
                    proteinsLabel: formatMealManageMacro(totals.proteins, 'GENERAL.UNITS.G', this.translateService),
                    fatsLabel: formatMealManageMacro(totals.fats, 'GENERAL.UNITS.G', this.translateService),
                    carbsLabel: formatMealManageMacro(totals.carbs, 'GENERAL.UNITS.G', this.translateService),
                };
            });
    });
    private readonly activeLang = signal(this.translateService.getCurrentLang());

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }

    private getItemSourceName(item: MealItemFormValues): string {
        const isRecipe = item.sourceType === MealSourceType.Recipe;
        const name = isRecipe ? item.recipe?.name : item.product?.name;
        if (name !== undefined && name.trim().length > 0) {
            return name;
        }

        return this.translateService.instant(isRecipe ? 'MEAL_MANAGE.ITEM_TYPE_OPTIONS.Recipe' : 'MEAL_MANAGE.ITEM_TYPE_OPTIONS.Product');
    }

    private getManualItemTotals(item: MealItemFormValues): NutritionTotals {
        const amount = item.amount ?? 0;

        if (item.sourceType === MealSourceType.Product) {
            return this.getProductManualItemTotals(item.product, amount);
        }

        return this.getRecipeManualItemTotals(item.recipe, amount, item.recipeMass);
    }

    private getProductManualItemTotals(product: MealItemFormValues['product'], amount: number): NutritionTotals {
        if (product === null || product.baseAmount <= 0) {
            return getEmptyNutritionTotals();
        }

        const multiplier = amount / product.baseAmount;
        return {
            calories: product.caloriesPerBase * multiplier,
            proteins: product.proteinsPerBase * multiplier,
            fats: product.fatsPerBase * multiplier,
            carbs: product.carbsPerBase * multiplier,
            fiber: product.fiberPerBase * multiplier,
            alcohol: product.alcoholPerBase * multiplier,
        };
    }

    private getRecipeManualItemTotals(recipe: MealItemFormValues['recipe'], amount: number, mass: ServingMassResult): NutritionTotals {
        if (recipe === null || recipe.servings <= 0) {
            return getEmptyNutritionTotals();
        }

        const servingsAmount = recipeServingsFromInput(amount, mass);
        return {
            calories: ((recipe.totalCalories ?? 0) / recipe.servings) * servingsAmount,
            proteins: ((recipe.totalProteins ?? 0) / recipe.servings) * servingsAmount,
            fats: ((recipe.totalFats ?? 0) / recipe.servings) * servingsAmount,
            carbs: ((recipe.totalCarbs ?? 0) / recipe.servings) * servingsAmount,
            fiber: ((recipe.totalFiber ?? 0) / recipe.servings) * servingsAmount,
            alcohol: ((recipe.totalAlcohol ?? 0) / recipe.servings) * servingsAmount,
        };
    }

    private formatManualAmount(item: MealItemFormValues): string {
        const amount = item.amount ?? 0;
        const unitLabel = this.getAmountUnitLabelForItem(item, amount);
        return formatMealManageAmount(amount, unitLabel, this.translateService);
    }

    private getManualItemImageUrl(item: MealItemFormValues): string | null {
        if (item.sourceType === MealSourceType.Recipe) {
            return item.recipe?.imageUrl ?? null;
        }

        return item.product?.imageUrl ?? null;
    }

    protected onEditItem(index: number): void {
        this.editItem.emit(index);
    }

    protected onRemoveItem(item: MealItemFormValues): void {
        this.removeItemEvent.emit(item);
    }

    protected onItemSourceClick(index: number): void {
        this.openItemSelect.emit(index);
    }

    private getServingUnitLabel(amount: number): string {
        const isSingleServing = formatMealManageNumber(amount, this.translateService) === formatMealManageNumber(1, this.translateService);
        return this.translateService.instant(isSingleServing ? 'MEAL_MANAGE.AMOUNT_UNIT_SERVING' : 'MEAL_MANAGE.AMOUNT_UNIT_SERVINGS');
    }

    private getAmountUnitLabelForItem(item: MealItemFormValues, amount: number): string {
        if (item.product !== null) {
            return this.translateService.instant(`GENERAL.UNITS.${item.product.baseUnit.toUpperCase()}`);
        }

        return item.recipeMass.kind === 'known' ? this.translateService.instant('GENERAL.UNITS.G') : this.getServingUnitLabel(amount);
    }
}

export type MealItemsListItemState = {
    formItem: MealItemFormValues;
    amountError: string | null;
    productInvalid: boolean;
    recipeInvalid: boolean;
    sourceError: string | null;
};

type ManualItemRowViewModel = {
    index: number;
    formItem: MealItemFormValues;
    imageUrl: string | null;
    icon: string;
    sourceName: string;
    amountLabel: string;
    caloriesLabel: string;
    proteinsLabel: string;
    fatsLabel: string;
    carbsLabel: string;
};
