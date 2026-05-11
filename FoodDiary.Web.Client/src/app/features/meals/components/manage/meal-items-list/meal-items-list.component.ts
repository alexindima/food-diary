import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type AbstractControl, type FormArray, type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { RecipeServingWeightService } from '../../../lib/recipe-serving-weight.service';
import { ConsumptionSourceType } from '../../../models/meal.data';
import type { ConsumptionItemFormData, NutritionTotals } from '../base-meal-manage.types';

const NUTRITION_FRACTION_THRESHOLD = 0.01;

@Component({
    selector: 'fd-meal-items-list',
    templateUrl: './meal-items-list.component.html',
    styleUrls: ['../base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        NgOptimizedImage,
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
                    caloriesLabel: this.formatManualMacro(totals.calories, 'GENERAL.UNITS.KCAL'),
                    proteinsLabel: this.formatManualMacro(totals.proteins, 'GENERAL.UNITS.G'),
                    fatsLabel: this.formatManualMacro(totals.fats, 'GENERAL.UNITS.G'),
                    carbsLabel: this.formatManualMacro(totals.carbs, 'GENERAL.UNITS.G'),
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
            return unit !== undefined ? this.translateService.instant('PRODUCT_AMOUNT_UNITS.' + unit.toUpperCase()) : null;
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

    public getAmountPlaceholder(index: number): string {
        return this.isRecipeItem(index) ? 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_RECIPE' : 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_PRODUCT';
    }

    public getAmountControlError(index: number): string | null {
        const group = (this.formArray().controls as Array<FormGroup<ConsumptionItemFormData> | undefined>)[index];
        return this.resolveControlError(group?.controls.amount ?? null);
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
            return this.getEmptyTotals();
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
            return this.getEmptyTotals();
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

    private formatManualMacro(value: number, unitKey: string): string {
        const locale = this.getCurrentLanguage();
        const hasFraction = Math.abs(value % 1) > NUTRITION_FRACTION_THRESHOLD;
        const formatter = new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        });
        const unitLabel = this.translateService.instant(unitKey);
        return `${formatter.format(value)} ${unitLabel}`.trim();
    }

    private formatManualAmount(index: number): string {
        const amount = this.formArray().at(index).controls.amount.value ?? 0;
        const unitLabel = this.getAmountUnitLabel(index);
        return unitLabel !== null ? `${this.formatNumber(amount)} ${unitLabel}`.trim() : this.formatNumber(amount);
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

    private formatNumber(value: number): string {
        const locale = this.getCurrentLanguage();
        const hasFraction = Math.abs(value % 1) > NUTRITION_FRACTION_THRESHOLD;
        return new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        }).format(value);
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (control === null || control.invalid === false || control.touched === false) {
            return null;
        }

        if (control.errors?.['required'] === true) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        const minError = control.getError('min') as { min?: number } | null;
        if (minError !== null) {
            const min = minError.min ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private getEmptyTotals(): NutritionTotals {
        return { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 };
    }

    private getCurrentLanguage(): string {
        const currentLang = this.translateService.getCurrentLang();
        if (currentLang.length > 0) {
            return currentLang;
        }

        const fallbackLang = this.translateService.getFallbackLang() ?? '';
        return fallbackLang.length > 0 ? fallbackLang : 'en';
    }
}

interface ManualItemRowViewModel {
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
}
