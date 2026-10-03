import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, min, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import type { Subscription } from 'rxjs';

import { ItemSelectDialogComponent } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog';
import type { ItemSelection } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog-lib/item-select-dialog.types';
import { MealSourceType } from '../../../../../shared/models/meal.data';
import type { Product } from '../../../../../shared/models/product.data';
import type { Recipe } from '../../../../../shared/models/recipe.data';
import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import type { MealItemFormValues } from '../meal-manage-lib/meal-manage.types';

const MIN_AMOUNT = 0.01;
const PRODUCT_SOURCE_VALUE = 'Product';
const RECIPE_SOURCE_VALUE = 'Recipe';

export type MealManualItemDialogData = {
    item: MealItemFormValues;
};

@Component({
    selector: 'fd-meal-manual-item-dialog',
    templateUrl: './meal-manual-item-dialog.html',
    styleUrls: ['./meal-manual-item-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiIconComponent,
        FdUiInputComponent,
        ItemSelectDialogComponent,
    ],
})
export class MealManualItemDialogComponent {
    private readonly destroyRef = inject(DestroyRef);
    private servingWeightSubscription?: Subscription;
    private readonly data = inject<MealManualItemDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject(FdUiDialogRef<MealManualItemDialogComponent, MealItemFormValues | null>);
    private readonly recipeWeight = inject(RecipeServingWeightService);
    private readonly translateService = inject(TranslateService);

    protected readonly showPicker = signal(this.data.item.product === null && this.data.item.recipe === null);
    protected readonly amountLabel = computed(() => {
        const unit = this.product()?.baseUnit ?? 'G';
        return `${this.translateService.instant('MEAL_MANAGE.ADD_ITEM_ROW.AMOUNT')} (${this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${unit.toUpperCase()}`)})`;
    });
    protected readonly sourceType = signal(this.data.item.sourceType);
    protected readonly sourceTypeValue = signal(this.toSourceTypeValue(this.data.item.sourceType));
    protected readonly product = signal<Product | null>(this.data.item.product);
    protected readonly recipe = signal<Recipe | null>(this.data.item.recipe);
    private readonly sourceTouched = signal(false);
    protected readonly amountModel = signal<number | null>(this.data.item.amount);
    protected readonly amount = form(this.amountModel, path => {
        required(path);
        min(path, MIN_AMOUNT);
    });

    protected readonly selectedItemName = computed(() => this.recipe()?.name ?? this.product()?.name ?? null);
    protected readonly itemSourceName = computed(() => this.selectedItemName() ?? '');

    protected readonly sourceActionLabelKey = computed(() =>
        this.sourceType() === MealSourceType.Recipe ? 'MEAL_MANAGE.MANUAL_ITEM_CHOOSE_RECIPE' : 'MEAL_MANAGE.MANUAL_ITEM_CHOOSE_PRODUCT',
    );

    protected readonly sourceTypeLabelKey = computed(() =>
        this.sourceType() === MealSourceType.Recipe ? 'MEAL_MANAGE.ITEM_TYPE_OPTIONS.Recipe' : 'MEAL_MANAGE.ITEM_TYPE_OPTIONS.Product',
    );

    protected readonly selectedItemMeta = computed(() => {
        const recipe = this.recipe();
        if (recipe !== null) {
            const calories = recipe.manualCalories ?? recipe.totalCalories;
            return calories === null || calories === undefined
                ? this.translateService.instant('MEAL_MANAGE.ITEM_TYPE_OPTIONS.Recipe')
                : this.translateService.instant('MEAL_MANAGE.MANUAL_ITEM_RECIPE_META', { calories: Math.round(calories) });
        }

        const product = this.product();
        if (product !== null) {
            return this.translateService.instant('MEAL_MANAGE.MANUAL_ITEM_PRODUCT_META', {
                amount: product.baseAmount,
                unit: this.translateService.instant(`PRODUCT_AMOUNT_UNITS_SHORT.${product.baseUnit.toUpperCase()}`),
                calories: Math.round(product.caloriesPerBase),
            });
        }

        return null;
    });

    protected readonly itemSourceIcon = computed(() => {
        if (this.recipe() !== null) {
            return 'menu_book';
        }
        if (this.product() !== null) {
            return 'restaurant';
        }
        return 'search';
    });

    protected readonly amountPlaceholderKey = computed(() =>
        this.sourceType() === MealSourceType.Recipe ? 'MEAL_MANAGE.AMOUNT_PLACEHOLDER_RECIPE' : 'MEAL_MANAGE.AMOUNT_PLACEHOLDER_PRODUCT',
    );

    protected readonly sourceError = computed(() => {
        if (!this.sourceTouched() || this.product() !== null || this.recipe() !== null) {
            return null;
        }

        return this.translateService.instant('MEAL_MANAGE.ITEM_SOURCE_ERROR');
    });

    protected readonly amountError = computed(() => {
        const state = this.amount();
        if (!state.invalid() || !state.touched()) {
            return null;
        }

        if (state.getError('required') !== undefined) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (state.getError('min') !== undefined) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min: MIN_AMOUNT });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    });

    protected readonly canSave = computed(() => (this.product() !== null || this.recipe() !== null) && !this.amount().invalid());

    protected onSourceTypeChange(value: string): void {
        const nextSourceType = value === RECIPE_SOURCE_VALUE ? MealSourceType.Recipe : MealSourceType.Product;
        if (nextSourceType === this.sourceType()) {
            this.sourceTypeValue.set(this.toSourceTypeValue(nextSourceType));
            return;
        }

        this.servingWeightSubscription?.unsubscribe();
        this.sourceType.set(nextSourceType);
        this.sourceTypeValue.set(this.toSourceTypeValue(nextSourceType));
        this.sourceTouched.set(false);
        this.product.set(null);
        this.recipe.set(null);
        this.amount().value.set(null);
    }

    protected showItemPicker(): void {
        this.showPicker.set(true);
    }

    protected onProductSelected(product: Product): void {
        this.applySelection({ type: 'Product', product });
    }

    protected onRecipeSelected(recipe: Recipe): void {
        this.applySelection({ type: 'Recipe', recipe });
    }

    private applySelection(selection: Exclude<ItemSelection, { type: 'Text' }>): void {
        this.showPicker.set(false);
        this.servingWeightSubscription?.unsubscribe();
        if (selection.type === 'Product') {
            this.sourceType.set(MealSourceType.Product);
            this.sourceTypeValue.set(PRODUCT_SOURCE_VALUE);
            this.product.set(selection.product);
            this.recipe.set(null);
            this.amount().value.set(this.resolveProductAmount(selection.product));
            return;
        }

        this.sourceType.set(MealSourceType.Recipe);
        this.sourceTypeValue.set(RECIPE_SOURCE_VALUE);
        this.recipe.set(selection.recipe);
        this.product.set(null);
        this.amount().value.set(1);
        this.loadRecipeAmount(selection.recipe);
    }

    private loadRecipeAmount(recipe: Recipe): void {
        this.servingWeightSubscription = this.recipeWeight
            .loadServingWeight(recipe)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(servingWeight => {
                if (servingWeight !== null && Number.isFinite(servingWeight) && servingWeight > 0 && this.amountModel() === 1) {
                    this.amount().value.set(servingWeight);
                }
            });
    }

    protected save(): void {
        this.sourceTouched.set(true);
        this.amount().markAsTouched();

        if (this.product() === null && this.recipe() === null) {
            return;
        }

        if (this.amount().invalid()) {
            return;
        }

        this.dialogRef.close({
            sourceType: this.sourceType(),
            product: this.product(),
            recipe: this.recipe(),
            amount: this.amountModel(),
        });
    }

    protected cancel(): void {
        this.dialogRef.close(null);
    }

    private resolveProductAmount(product: Product): number {
        if (Number.isFinite(product.defaultPortionAmount) && product.defaultPortionAmount > 0) {
            return product.defaultPortionAmount;
        }

        if (Number.isFinite(product.baseAmount) && product.baseAmount > 0) {
            return product.baseAmount;
        }

        return 1;
    }

    private toSourceTypeValue(sourceType: MealSourceType): string {
        return sourceType === MealSourceType.Recipe ? RECIPE_SOURCE_VALUE : PRODUCT_SOURCE_VALUE;
    }
}
