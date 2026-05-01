import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { AbstractControl, FormArray, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { ConsumptionSourceType } from '../../../models/meal.data';
import { ConsumptionItemFormData } from '../base-meal-manage.types';

@Component({
    selector: 'fd-meal-items-list',
    templateUrl: './meal-items-list.component.html',
    styleUrls: ['../base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        FormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
    ],
})
export class MealItemsListComponent {
    private readonly translateService = inject(TranslateService);

    public readonly formArray = input.required<FormArray<FormGroup<ConsumptionItemFormData>>>();
    public readonly isEditMode = input<boolean>(false);

    public readonly addItem = output<void>();
    public readonly removeItemEvent = output<number>();
    public readonly openItemSelect = output<number>();

    public isProductItem(index: number): boolean {
        return this.formArray().at(index).controls.sourceType.value === ConsumptionSourceType.Product;
    }

    public isRecipeItem(index: number): boolean {
        return this.formArray().at(index).controls.sourceType.value === ConsumptionSourceType.Recipe;
    }

    public getProductName(index: number): string {
        const control = this.formArray().at(index).controls.product;
        return control.value?.name || '';
    }

    public getRecipeName(index: number): string {
        const control = this.formArray().at(index).controls.recipe;
        return control.value?.name || '';
    }

    public getAmountUnitLabel(index: number): string | null {
        if (this.isProductItem(index)) {
            const unit = this.formArray().at(index).controls.product.value?.baseUnit;
            return unit ? this.translateService.instant('PRODUCT_AMOUNT_UNITS.' + unit.toUpperCase()) : null;
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

    public getItemSourceName(index: number): string {
        if (this.isRecipeItem(index)) {
            return this.getRecipeName(index);
        }
        return this.getProductName(index);
    }

    public getItemSourceIcon(index: number): string {
        if (this.isRecipeItem(index) && this.formArray().at(index).controls.recipe.value) {
            return 'menu_book';
        }
        if (this.isProductItem(index) && this.formArray().at(index).controls.product.value) {
            return 'restaurant';
        }
        return 'search';
    }

    public getAmountPlaceholder(index: number): string {
        return this.isRecipeItem(index) ? 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_RECIPE' : 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_PRODUCT';
    }

    public getAmountControlError(index: number): string | null {
        return this.resolveControlError(this.formArray().at(index)?.controls.amount ?? null);
    }

    public onAddItem(): void {
        this.addItem.emit();
    }

    public onRemoveItem(index: number): void {
        this.removeItemEvent.emit(index);
    }

    public onItemSourceClick(index: number): void {
        this.openItemSelect.emit(index);
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (!control || !control.invalid || !control.touched) {
            return null;
        }

        if (control.errors?.['required']) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (control.errors?.['min']) {
            const min = control.errors['min'].min ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }
}
