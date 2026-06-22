import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { form, FormField, min, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import { firstValueFrom } from 'rxjs';

import { ItemSelectDialogComponent } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog';
import type {
    ItemSelectDialogData,
    ItemSelection,
} from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog-lib/item-select-dialog.types';
import type { Product } from '../../../../products/models/product.data';
import type { Recipe } from '../../../../recipes/models/recipe.data';
import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import { ConsumptionSourceType } from '../../../models/meal.data';
import type { ConsumptionItemFormValues } from '../meal-manage-lib/meal-manage.types';

const MIN_AMOUNT = 0.01;
const PRODUCT_SOURCE_VALUE = 'Product';
const RECIPE_SOURCE_VALUE = 'Recipe';

export type MealManualItemDialogData = {
    item: ConsumptionItemFormValues;
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
        FdUiSegmentedToggleComponent,
    ],
})
export class MealManualItemDialogComponent {
    private readonly data = inject<MealManualItemDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject(FdUiDialogRef<MealManualItemDialogComponent, ConsumptionItemFormValues | null>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly recipeWeight = inject(RecipeServingWeightService);
    private readonly translateService = inject(TranslateService);

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

    protected readonly sourceTypeOptions: FdUiSegmentedToggleOption[] = [
        { value: PRODUCT_SOURCE_VALUE, label: this.translateService.instant('CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.Product') },
        { value: RECIPE_SOURCE_VALUE, label: this.translateService.instant('CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.Recipe') },
    ];

    protected readonly selectedItemName = computed(() => this.recipe()?.name ?? this.product()?.name ?? null);
    protected readonly itemSourceName = computed(() => this.selectedItemName() ?? '');

    protected readonly sourceActionLabelKey = computed(() =>
        this.sourceType() === ConsumptionSourceType.Recipe
            ? 'CONSUMPTION_MANAGE.MANUAL_ITEM_CHOOSE_RECIPE'
            : 'CONSUMPTION_MANAGE.MANUAL_ITEM_CHOOSE_PRODUCT',
    );

    protected readonly sourceTypeLabelKey = computed(() =>
        this.sourceType() === ConsumptionSourceType.Recipe
            ? 'CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.Recipe'
            : 'CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.Product',
    );

    protected readonly selectedItemMeta = computed(() => {
        const recipe = this.recipe();
        if (recipe !== null) {
            const calories = recipe.manualCalories ?? recipe.totalCalories;
            return calories === null || calories === undefined
                ? this.translateService.instant('CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.Recipe')
                : this.translateService.instant('CONSUMPTION_MANAGE.MANUAL_ITEM_RECIPE_META', { calories: Math.round(calories) });
        }

        const product = this.product();
        if (product !== null) {
            return this.translateService.instant('CONSUMPTION_MANAGE.MANUAL_ITEM_PRODUCT_META', {
                amount: product.baseAmount,
                unit: product.baseUnit,
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
        this.sourceType() === ConsumptionSourceType.Recipe
            ? 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_RECIPE'
            : 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_PRODUCT',
    );

    protected readonly sourceError = computed(() => {
        if (!this.sourceTouched() || this.product() !== null || this.recipe() !== null) {
            return null;
        }

        return this.translateService.instant('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR');
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
        const nextSourceType = value === RECIPE_SOURCE_VALUE ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product;
        if (nextSourceType === this.sourceType()) {
            this.sourceTypeValue.set(this.toSourceTypeValue(nextSourceType));
            return;
        }

        this.sourceType.set(nextSourceType);
        this.sourceTypeValue.set(this.toSourceTypeValue(nextSourceType));
        this.sourceTouched.set(false);
        this.product.set(null);
        this.recipe.set(null);
        this.amount().value.set(null);
    }

    protected async chooseItemAsync(): Promise<void> {
        const initialTab = this.sourceType() === ConsumptionSourceType.Recipe ? RECIPE_SOURCE_VALUE : PRODUCT_SOURCE_VALUE;
        const selection = await firstValueFrom(
            this.fdDialogService
                .open<ItemSelectDialogComponent, ItemSelectDialogData, ItemSelection | null>(ItemSelectDialogComponent, {
                    preset: 'list',
                    data: { initialTab, lockInitialTab: true },
                })
                .afterClosed(),
        );

        if (selection === null || selection === undefined) {
            return;
        }

        if (selection.type === 'Product') {
            this.sourceType.set(ConsumptionSourceType.Product);
            this.sourceTypeValue.set(PRODUCT_SOURCE_VALUE);
            this.product.set(selection.product);
            this.recipe.set(null);
            this.amount().value.set(this.resolveProductAmount(selection.product));
            return;
        }

        this.sourceType.set(ConsumptionSourceType.Recipe);
        this.sourceTypeValue.set(RECIPE_SOURCE_VALUE);
        this.recipe.set(selection.recipe);
        this.product.set(null);
        this.amount().value.set(1);
        this.recipeWeight.loadServingWeight(selection.recipe).subscribe(servingWeight => {
            if (servingWeight !== null && servingWeight > 0) {
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
        if (product.defaultPortionAmount > 0) {
            return product.defaultPortionAmount;
        }

        if (product.baseAmount > 0) {
            return product.baseAmount;
        }

        return 1;
    }

    private toSourceTypeValue(sourceType: ConsumptionSourceType): string {
        return sourceType === ConsumptionSourceType.Recipe ? RECIPE_SOURCE_VALUE : PRODUCT_SOURCE_VALUE;
    }
}
