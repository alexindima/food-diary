import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, type FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { firstValueFrom, merge } from 'rxjs';

import {
    ItemSelectDialogComponent,
    type ItemSelectDialogData,
    type ItemSelection,
} from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog.component';
import type { Product } from '../../../../products/models/product.data';
import type { Recipe } from '../../../../recipes/models/recipe.data';
import { MealManageFacade } from '../../../lib/meal-manage.facade';
import { RecipeServingWeightService } from '../../../lib/recipe-serving-weight.service';
import { ConsumptionSourceType } from '../../../models/meal.data';
import type { ConsumptionItemFormData } from '../meal-manage.types';
import { resolveMealManageControlError } from '../meal-manage-view.utils';

const MIN_AMOUNT = 0.01;

export type MealManualItemDialogData = {
    group: FormGroup<ConsumptionItemFormData>;
};

@Component({
    selector: 'fd-meal-manual-item-dialog',
    templateUrl: './meal-manual-item-dialog.component.html',
    styleUrls: ['./meal-manual-item-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        FormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiInputComponent,
    ],
})
export class MealManualItemDialogComponent {
    private readonly data = inject<MealManualItemDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject(FdUiDialogRef<MealManualItemDialogComponent, boolean>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly recipeWeight = inject(RecipeServingWeightService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly amountValidationVersion = signal(0);

    public readonly sourceType = signal(this.data.group.controls.sourceType.value);
    public readonly product = signal<Product | null>(this.data.group.controls.product.value);
    public readonly recipe = signal<Recipe | null>(this.data.group.controls.recipe.value);
    public readonly amount = new FormControl<number | null>(this.data.group.controls.amount.value, {
        validators: [Validators.required, Validators.min(MIN_AMOUNT)],
    });

    public readonly itemSourceName = computed(() => this.recipe()?.name ?? this.product()?.name ?? '');

    public readonly itemSourceIcon = computed(() => {
        if (this.recipe() !== null) {
            return 'menu_book';
        }
        if (this.product() !== null) {
            return 'restaurant';
        }
        return 'search';
    });

    public readonly amountPlaceholderKey = computed(() =>
        this.sourceType() === ConsumptionSourceType.Recipe
            ? 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_RECIPE'
            : 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_PRODUCT',
    );

    public readonly sourceError = computed(() =>
        this.product() !== null || this.recipe() !== null ? null : this.translateService.instant('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR'),
    );

    public readonly amountError = computed(() => {
        this.amountValidationVersion();
        return resolveMealManageControlError(this.amount, this.translateService, MIN_AMOUNT);
    });

    public constructor() {
        merge(this.amount.statusChanges, this.amount.valueChanges)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.refreshAmountValidation();
            });
    }

    public async chooseItemAsync(): Promise<void> {
        const initialTab = this.sourceType() === ConsumptionSourceType.Recipe ? 'Recipe' : 'Product';
        const selection = await firstValueFrom(
            this.fdDialogService
                .open<ItemSelectDialogComponent, ItemSelectDialogData, ItemSelection | null>(ItemSelectDialogComponent, {
                    preset: 'list',
                    data: { initialTab },
                })
                .afterClosed(),
        );

        if (selection === null || selection === undefined) {
            return;
        }

        if (selection.type === 'Product') {
            this.sourceType.set(ConsumptionSourceType.Product);
            this.product.set(selection.product);
            this.recipe.set(null);
            this.amount.setValue(this.resolveProductAmount(selection.product));
            this.amount.markAsUntouched();
            this.refreshAmountValidation();
            return;
        }

        this.sourceType.set(ConsumptionSourceType.Recipe);
        this.recipe.set(selection.recipe);
        this.product.set(null);
        this.amount.setValue(1);
        this.amount.markAsUntouched();
        this.refreshAmountValidation();
        this.recipeWeight.loadServingWeight(selection.recipe).subscribe(servingWeight => {
            if (servingWeight !== null && servingWeight > 0) {
                this.amount.setValue(servingWeight);
            }
        });
    }

    public save(): void {
        this.amount.markAsTouched();
        this.refreshAmountValidation();

        if (this.product() === null && this.recipe() === null) {
            return;
        }

        if (this.amount.invalid) {
            return;
        }

        this.data.group.patchValue({
            sourceType: this.sourceType(),
            product: this.product(),
            recipe: this.recipe(),
            amount: this.amount.value,
        });
        this.mealManageFacade.configureItemType(this.data.group, this.sourceType());
        this.dialogRef.close(true);
    }

    public cancel(): void {
        this.dialogRef.close(false);
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

    private refreshAmountValidation(): void {
        this.amountValidationVersion.update(version => version + 1);
    }
}
