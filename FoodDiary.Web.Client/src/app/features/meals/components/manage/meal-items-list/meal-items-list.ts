import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import { ConsumptionSourceType } from '../../../models/meal.data';
import type { ConsumptionItemFormValues, NutritionTotals } from '../meal-manage-lib/meal-manage.types';
import { formatMealManageAmount, formatMealManageMacro, getEmptyNutritionTotals } from '../meal-manage-lib/meal-manage-view.utils';

@Component({
    selector: 'fd-meal-items-list',
    templateUrl: './meal-items-list.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiHintDirective, FdUiCardComponent, FdUiButtonComponent, FdUiFormErrorComponent, FdUiIconComponent],
})
export class MealItemsListComponent {
    private readonly translateService = inject(TranslateService);
    private readonly recipeWeight = inject(RecipeServingWeightService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly items = input.required<readonly MealItemsListItemState[]>();
    public readonly hasExternalItems = input.required<boolean>();
    public readonly arrayError = input<string | null>(null);
    public readonly renderVersion = input<number>(0);

    public readonly editItem = output<number>();
    public readonly removeItemEvent = output<number>();
    public readonly openItemSelect = output<number>();
    protected readonly manualItemRows = computed<ManualItemRowViewModel[]>(() => {
        this.renderVersion();
        this.activeLang();

        return this.items()
            .map((item, index) => ({ index, item }))
            .filter(({ item }) => this.hasManualItem(item))
            .map(({ index, item }) => {
                const totals = this.getManualItemTotals(item);

                return {
                    index,
                    imageUrl: this.getManualItemImageUrl(item),
                    icon: this.getItemSourceIcon(item),
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

    protected isProductItem(index: number): boolean {
        return this.getItem(index)?.sourceType === ConsumptionSourceType.Product;
    }

    protected isRecipeItem(index: number): boolean {
        return this.getItem(index)?.sourceType === ConsumptionSourceType.Recipe;
    }

    protected getProductName(index: number): string {
        return this.getItem(index)?.product?.name ?? '';
    }

    protected getRecipeName(index: number): string {
        return this.getItem(index)?.recipe?.name ?? '';
    }

    protected getAmountUnitLabel(index: number): string | null {
        if (this.isProductItem(index)) {
            const unit = this.getItem(index)?.product?.baseUnit;
            return unit !== undefined ? this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${unit.toUpperCase()}`) : null;
        }

        if (this.isRecipeItem(index)) {
            return this.translateService.instant('PRODUCT_AMOUNT_UNITS.G');
        }

        return null;
    }

    protected isProductInvalid(index: number): boolean {
        return this.getItem(index)?.productInvalid ?? false;
    }

    protected isRecipeInvalid(index: number): boolean {
        return this.getItem(index)?.recipeInvalid ?? false;
    }

    protected isItemSourceInvalid(index: number): boolean {
        return this.isProductInvalid(index) || this.isRecipeInvalid(index);
    }

    protected getItemSourceError(index: number): string | null {
        return this.getItem(index)?.sourceError ?? null;
    }

    private getItemSourceName(item: MealItemsListItemState): string {
        if (item.sourceType === ConsumptionSourceType.Recipe) {
            return item.recipe?.name ?? '';
        }
        return item.product?.name ?? '';
    }

    private getItemSourceIcon(item: MealItemsListItemState): string {
        if (item.sourceType === ConsumptionSourceType.Recipe && item.recipe !== null) {
            return 'menu_book';
        }
        if (item.sourceType === ConsumptionSourceType.Product && item.product !== null) {
            return 'restaurant';
        }
        return 'search';
    }

    protected getAmountControlError(index: number): string | null {
        return this.getItem(index)?.amountError ?? null;
    }

    private getManualItemTotals(item: MealItemsListItemState): NutritionTotals {
        const amount = item.amount ?? 0;

        if (item.sourceType === ConsumptionSourceType.Product) {
            return this.getProductManualItemTotals(item.product, amount);
        }

        return this.getRecipeManualItemTotals(item.recipe, amount);
    }

    private getProductManualItemTotals(product: ConsumptionItemFormValues['product'], amount: number): NutritionTotals {
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

    private getRecipeManualItemTotals(recipe: ConsumptionItemFormValues['recipe'], amount: number): NutritionTotals {
        if (recipe === null || recipe.servings <= 0) {
            return getEmptyNutritionTotals();
        }

        const servingsAmount = this.recipeWeight.convertGramsToServings(recipe, amount);
        return {
            calories: ((recipe.totalCalories ?? 0) / recipe.servings) * servingsAmount,
            proteins: ((recipe.totalProteins ?? 0) / recipe.servings) * servingsAmount,
            fats: ((recipe.totalFats ?? 0) / recipe.servings) * servingsAmount,
            carbs: ((recipe.totalCarbs ?? 0) / recipe.servings) * servingsAmount,
            fiber: ((recipe.totalFiber ?? 0) / recipe.servings) * servingsAmount,
            alcohol: ((recipe.totalAlcohol ?? 0) / recipe.servings) * servingsAmount,
        };
    }

    private formatManualAmount(item: MealItemsListItemState): string {
        const amount = item.amount ?? 0;
        const unitLabel = this.getAmountUnitLabelForItem(item);
        return formatMealManageAmount(amount, unitLabel, this.translateService);
    }

    private getManualItemImageUrl(item: MealItemsListItemState): string | null {
        if (item.sourceType === ConsumptionSourceType.Recipe) {
            return item.recipe?.imageUrl ?? null;
        }

        return item.product?.imageUrl ?? null;
    }

    protected onEditItem(index: number): void {
        this.editItem.emit(index);
    }

    protected hasManualItem(index: number): boolean;
    protected hasManualItem(item: MealItemsListItemState): boolean;
    protected hasManualItem(value: number | MealItemsListItemState): boolean {
        this.renderVersion();
        const item = typeof value === 'number' ? this.getItem(value) : value;
        return item !== undefined && (item.product !== null || item.recipe !== null);
    }

    protected onRemoveItem(index: number): void {
        this.removeItemEvent.emit(index);
    }

    protected onItemSourceClick(index: number): void {
        this.openItemSelect.emit(index);
    }

    private getItem(index: number): MealItemsListItemState | undefined {
        return this.items()[index];
    }

    private getAmountUnitLabelForItem(item: MealItemsListItemState): string | null {
        if (item.sourceType === ConsumptionSourceType.Product) {
            const unit = item.product?.baseUnit;
            return unit !== undefined ? this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${unit.toUpperCase()}`) : null;
        }

        return this.translateService.instant('PRODUCT_AMOUNT_UNITS.G');
    }
}

export type MealItemsListItemState = ConsumptionItemFormValues & {
    amountError: string | null;
    productInvalid: boolean;
    recipeInvalid: boolean;
    sourceError: string | null;
};

type ManualItemRowViewModel = {
    index: number;
    imageUrl: string | null;
    icon: string;
    sourceName: string;
    amountLabel: string;
    caloriesLabel: string;
    proteinsLabel: string;
    fatsLabel: string;
    carbsLabel: string;
};
