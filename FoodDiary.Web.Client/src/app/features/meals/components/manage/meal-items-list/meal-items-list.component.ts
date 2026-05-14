import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormArray, type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import { ConsumptionSourceType } from '../../../models/meal.data';
import type { ConsumptionItemFormData, NutritionTotals } from '../meal-manage-lib/meal-manage.types';
import {
    formatMealManageAmount,
    formatMealManageMacro,
    getEmptyNutritionTotals,
    resolveMealManageControlError,
} from '../meal-manage-lib/meal-manage-view.utils';

@Component({
    selector: 'fd-meal-items-list',
    templateUrl: './meal-items-list.component.html',
    styleUrls: ['../meal-manage-form.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        FdUiIconComponent,
    ],
})
export class MealItemsListComponent {
    private readonly translateService = inject(TranslateService);
    private readonly recipeWeight = inject(RecipeServingWeightService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly formArray = input.required<FormArray<FormGroup<ConsumptionItemFormData>>>();
    public readonly hasExternalItems = input.required<boolean>();
    public readonly renderVersion = input<number>(0);

    public readonly editItem = output<number>();
    public readonly removeItemEvent = output<number>();
    public readonly openItemSelect = output<number>();
    public readonly manualItemRows = computed<ManualItemRowViewModel[]>(() => {
        this.renderVersion();
        this.activeLang();

        return this.formArray()
            .controls.map((group, index) => ({ group, index }))
            .filter(({ index }) => this.hasManualItem(index))
            .map(({ group, index }) => {
                const totals = this.getManualItemTotals(index);

                return {
                    index,
                    group,
                    imageUrl: this.getManualItemImageUrl(index),
                    icon: this.getItemSourceIcon(index),
                    sourceName: this.getItemSourceName(index),
                    amountLabel: this.formatManualAmount(index),
                    caloriesLabel: formatMealManageMacro(totals.calories, 'GENERAL.UNITS.KCAL', this.translateService),
                    proteinsLabel: formatMealManageMacro(totals.proteins, 'GENERAL.UNITS.G', this.translateService),
                    fatsLabel: formatMealManageMacro(totals.fats, 'GENERAL.UNITS.G', this.translateService),
                    carbsLabel: formatMealManageMacro(totals.carbs, 'GENERAL.UNITS.G', this.translateService),
                };
            });
    });
    public readonly arrayError = computed(() =>
        this.formArray().touched && this.formArray().errors?.['nonEmptyArray'] === true
            ? this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY')
            : null,
    );

    private readonly activeLang = signal(this.translateService.getCurrentLang());

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }

    public isProductItem(index: number): boolean {
        return this.formArray().at(index).controls.sourceType.value === ConsumptionSourceType.Product;
    }

    public isRecipeItem(index: number): boolean {
        return this.formArray().at(index).controls.sourceType.value === ConsumptionSourceType.Recipe;
    }

    public getProductName(index: number): string {
        const control = this.formArray().at(index).controls.product;
        return control.value?.name ?? '';
    }

    public getRecipeName(index: number): string {
        const control = this.formArray().at(index).controls.recipe;
        return control.value?.name ?? '';
    }

    public getAmountUnitLabel(index: number): string | null {
        if (this.isProductItem(index)) {
            const unit = this.formArray().at(index).controls.product.value?.baseUnit;
            return unit !== undefined ? this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${unit.toUpperCase()}`) : null;
        }

        if (this.isRecipeItem(index)) {
            return this.translateService.instant('PRODUCT_AMOUNT_UNITS.G');
        }

        return null;
    }

    public isProductInvalid(index: number): boolean {
        if (!this.isProductItem(index)) {
            return false;
        }
        const control = this.formArray().at(index).controls.product;
        return control.invalid && control.touched;
    }

    public isRecipeInvalid(index: number): boolean {
        if (!this.isRecipeItem(index)) {
            return false;
        }
        const control = this.formArray().at(index).controls.recipe;
        return control.invalid && control.touched;
    }

    public isItemSourceInvalid(index: number): boolean {
        return this.isProductInvalid(index) || this.isRecipeInvalid(index);
    }

    public getItemSourceError(index: number): string | null {
        return this.isItemSourceInvalid(index) ? this.translateService.instant('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR') : null;
    }

    private getItemSourceName(index: number): string {
        if (this.isRecipeItem(index)) {
            return this.getRecipeName(index);
        }
        return this.getProductName(index);
    }

    private getItemSourceIcon(index: number): string {
        if (this.isRecipeItem(index) && this.formArray().at(index).controls.recipe.value !== null) {
            return 'menu_book';
        }
        if (this.isProductItem(index) && this.formArray().at(index).controls.product.value !== null) {
            return 'restaurant';
        }
        return 'search';
    }

    public getAmountControlError(index: number): string | null {
        const group = (this.formArray().controls as Array<FormGroup<ConsumptionItemFormData> | undefined>)[index];
        return resolveMealManageControlError(group?.controls.amount ?? null, this.translateService);
    }

    private getManualItemTotals(index: number): NutritionTotals {
        const group = this.formArray().at(index);
        const amount = group.controls.amount.value ?? 0;

        if (group.controls.sourceType.value === ConsumptionSourceType.Product) {
            return this.getProductManualItemTotals(group.controls.product.value, amount);
        }

        return this.getRecipeManualItemTotals(group.controls.recipe.value, amount);
    }

    private getProductManualItemTotals(product: ConsumptionItemFormData['product']['value'], amount: number): NutritionTotals {
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

    private getRecipeManualItemTotals(recipe: ConsumptionItemFormData['recipe']['value'], amount: number): NutritionTotals {
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

    private formatManualAmount(index: number): string {
        const amount = this.formArray().at(index).controls.amount.value ?? 0;
        const unitLabel = this.getAmountUnitLabel(index);
        return formatMealManageAmount(amount, unitLabel, this.translateService);
    }

    private getManualItemImageUrl(index: number): string | null {
        if (this.isRecipeItem(index)) {
            return this.formArray().at(index).controls.recipe.value?.imageUrl ?? null;
        }

        return this.formArray().at(index).controls.product.value?.imageUrl ?? null;
    }

    public onEditItem(index: number): void {
        this.editItem.emit(index);
    }

    public hasManualItem(index: number): boolean {
        this.renderVersion();
        const group = this.formArray().at(index);
        return group.controls.product.value !== null || group.controls.recipe.value !== null;
    }

    public onRemoveItem(index: number): void {
        this.removeItemEvent.emit(index);
    }

    public onItemSourceClick(index: number): void {
        this.openItemSelect.emit(index);
    }
}

type ManualItemRowViewModel = {
    index: number;
    group: FormGroup<ConsumptionItemFormData>;
    imageUrl: string | null;
    icon: string;
    sourceName: string;
    amountLabel: string;
    caloriesLabel: string;
    proteinsLabel: string;
    fatsLabel: string;
    carbsLabel: string;
};
