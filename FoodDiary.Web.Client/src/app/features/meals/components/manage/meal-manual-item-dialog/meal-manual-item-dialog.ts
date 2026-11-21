import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, max, min, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import type { Subscription } from 'rxjs';

import { ItemSelectDialogComponent } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog';
import type { ItemSelection } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog-lib/item-select-dialog.types';
import { MealSourceType } from '../../../../../shared/models/meal.data';
import type { Product } from '../../../../../shared/models/product.data';
import type { Recipe } from '../../../../../shared/models/recipe.data';
import { MAXIMUM_CONSUMED_QUANTITY, recipeServings } from '../../../../../shared/models/semantics/meal-quantity';
import { displayRecipeServings, type ServingMassResult, UNKNOWN_SERVING_MASS } from '../../../lib/recipe-serving/recipe-display-amount';
import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import type { MealItemFormValues } from '../meal-manage-lib/meal-manage.types';
import { createMealItemValue } from '../meal-manage-lib/meal-manage-form.mapper';

const MIN_AMOUNT = 0.01;

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
    private readonly quantityErrors: FdValidationErrors = {
        ...inject(FD_VALIDATION_ERRORS),
        max: () => ({ key: 'MEAL_MANAGE.MAX_AMOUNT_ERROR', params: { max: this.maximumAmount() } }),
    };

    protected readonly showPicker = signal(this.data.item.product === null && this.data.item.recipe === null);
    protected readonly isLoadingRecipeAmount = signal(false);
    private readonly recipeMass = signal<ServingMassResult>(this.data.item.recipeMass);
    private readonly usesRecipeGrams = computed(() => this.recipeMass().kind === 'known');
    protected readonly amountLabel = computed(() => {
        if (this.recipe() !== null && !this.usesRecipeGrams()) {
            return this.translateService.instant('MEAL_MANAGE.AMOUNT_LABEL_RECIPE_SERVINGS');
        }
        const unit = this.product()?.baseUnit ?? 'G';
        return `${this.translateService.instant('MEAL_MANAGE.ADD_ITEM_ROW.AMOUNT')} (${this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${unit.toUpperCase()}`)})`;
    });
    protected readonly sourceType = signal(this.data.item.sourceType);
    protected readonly product = signal<Product | null>(this.data.item.product);
    protected readonly recipe = signal<Recipe | null>(this.data.item.recipe);
    private readonly sourceTouched = signal(false);
    protected readonly amountModel = signal<number | null>(this.data.item.amount);
    protected readonly maximumAmount = computed(() =>
        this.recipe() !== null && this.usesRecipeGrams()
            ? displayRecipeServings(recipeServings(MAXIMUM_CONSUMED_QUANTITY), this.recipeMass()).value
            : MAXIMUM_CONSUMED_QUANTITY,
    );
    protected readonly amount = form(this.amountModel, path => {
        disabled(path, { when: () => this.isLoadingRecipeAmount() });
        required(path);
        min(path, MIN_AMOUNT);
        max(path, () => this.maximumAmount());
    });

    protected readonly sourceActionLabelKey = computed(() =>
        this.sourceType() === MealSourceType.Recipe ? 'MEAL_MANAGE.MANUAL_ITEM_CHOOSE_RECIPE' : 'MEAL_MANAGE.MANUAL_ITEM_CHOOSE_PRODUCT',
    );

    protected readonly sourceTypeLabelKey = computed(() =>
        this.sourceType() === MealSourceType.Recipe ? 'MEAL_MANAGE.ITEM_TYPE_OPTIONS.Recipe' : 'MEAL_MANAGE.ITEM_TYPE_OPTIONS.Product',
    );

    protected readonly selectedItem = computed(() => {
        const recipe = this.recipe();
        if (recipe !== null) {
            const totalCalories = recipe.manualCalories ?? recipe.totalCalories;
            const calories = totalCalories === null || totalCalories === undefined ? null : totalCalories / Math.max(recipe.servings, 1);
            return {
                name: recipe.name,
                icon: 'menu_book',
                meta:
                    calories === null
                        ? this.translateService.instant('MEAL_MANAGE.ITEM_TYPE_OPTIONS.Recipe')
                        : this.translateService.instant('MEAL_MANAGE.MANUAL_ITEM_RECIPE_META', { calories: Math.round(calories) }),
            };
        }

        const product = this.product();
        if (product === null) {
            return null;
        }
        return {
            name: product.name,
            icon: 'restaurant',
            meta: this.translateService.instant('MEAL_MANAGE.MANUAL_ITEM_PRODUCT_META', {
                amount: product.baseAmount,
                unit: this.translateService.instant(`PRODUCT_AMOUNT_UNITS_SHORT.${product.baseUnit.toUpperCase()}`),
                calories: Math.round(product.caloriesPerBase),
            }),
        };
    });

    protected readonly amountPlaceholderKey = computed(() => {
        if (this.sourceType() !== MealSourceType.Recipe) {
            return 'MEAL_MANAGE.AMOUNT_PLACEHOLDER_PRODUCT';
        }
        return this.usesRecipeGrams() ? 'MEAL_MANAGE.AMOUNT_PLACEHOLDER_RECIPE' : 'MEAL_MANAGE.AMOUNT_PLACEHOLDER_RECIPE_SERVINGS';
    });

    protected readonly sourceError = computed(() => {
        if (!this.sourceTouched() || this.product() !== null || this.recipe() !== null) {
            return null;
        }

        return this.translateService.instant('MEAL_MANAGE.ITEM_SOURCE_ERROR');
    });

    protected readonly amountError = computed(() =>
        resolveSignalFormFieldError(this.amount, this.quantityErrors, this.translateService, { showOnDirty: false }),
    );

    protected readonly canSave = computed(() => !this.isLoadingRecipeAmount() && this.selectedItem() !== null && !this.amount().invalid());

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
        this.isLoadingRecipeAmount.set(false);
        this.recipeMass.set(UNKNOWN_SERVING_MASS);
        if (selection.type === 'Product') {
            this.sourceType.set(MealSourceType.Product);
            this.product.set(selection.product);
            this.recipe.set(null);
            this.amount().value.set(this.resolveProductAmount(selection.product));
            return;
        }

        this.sourceType.set(MealSourceType.Recipe);
        this.recipe.set(selection.recipe);
        this.product.set(null);
        this.amount().value.set(1);
        this.loadRecipeAmount(selection.recipe);
    }

    private loadRecipeAmount(recipe: Recipe): void {
        this.isLoadingRecipeAmount.set(true);
        this.servingWeightSubscription = this.recipeWeight
            .loadServingMass(recipe)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(mass => {
                this.recipeMass.set(mass);
                if (mass.kind === 'known' && this.amountModel() === 1) {
                    this.amount().value.set(mass.gramsPerServing);
                }
                this.isLoadingRecipeAmount.set(false);
            });
    }

    protected save(): void {
        if (this.isLoadingRecipeAmount()) {
            return;
        }
        this.sourceTouched.set(true);
        this.amount().markAsTouched();

        if (this.product() === null && this.recipe() === null) {
            return;
        }

        if (this.amount().invalid()) {
            return;
        }

        const item = createMealItemValue(this.product(), this.recipe(), this.amountModel(), this.sourceType());
        this.dialogRef.close({ ...item, recipeMass: this.recipeMass() });
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
}
