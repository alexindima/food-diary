import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiAutocompleteComponent, type FdUiAutocompleteOption } from 'fd-ui-kit/autocomplete/fd-ui-autocomplete';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { getNumberProperty } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { EMPTY, type Observable } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../../models/product.data';
import type { ProductFormValues } from '../product-manage-lib/product-manage-form.types';
import type { ProductNameAutocompleteOption, ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';

const ERROR_FIELDS = ['name', 'productType', 'defaultPortionAmount', 'baseUnit', 'visibility'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-product-basic-info',
    templateUrl: './product-basic-info.html',
    styleUrls: ['./product-basic-info.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        TranslatePipe,
        FdUiAutocompleteComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiSelectComponent,
        FdUiCardComponent,
        ImageUploadFieldComponent,
    ],
})
export class ProductBasicInfoComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly form = input.required<FieldTree<ProductFormValues>>();
    public readonly nameOptions = input.required<ProductNameAutocompleteOption[]>();
    public readonly isNameSearchLoading = input.required<boolean>();
    protected readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());
    protected readonly unitOptions = signal<Array<FdUiSelectOption<MeasurementUnit>>>([]);
    protected readonly productTypeOptions = signal<Array<FdUiSelectOption<ProductType>>>([]);
    protected readonly visibilityOptions = signal<Array<FdUiSelectOption<ProductVisibility>>>([]);

    public readonly openBarcodeScanner = output();
    public readonly openAiRecognition = output();
    public readonly nameQueryChange = output<string>();
    public readonly nameSuggestionSelected = output<ProductNameSuggestion>();

    protected readonly displayNameValue = (value: string | null): string => value ?? '';

    public constructor() {
        effect(() => {
            this.fieldErrors.set(this.buildFieldErrors());
        });

        const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
        languageChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.refreshTranslatedState();
        });

        effect(() => {
            this.refreshTranslatedState();
        });
    }

    protected onNameOptionSelected(option: FdUiAutocompleteOption<string>): void {
        if (this.isProductNameSuggestion(option.data)) {
            this.nameSuggestionSelected.emit(option.data);
        }
    }

    private isProductNameSuggestion(value: unknown): value is ProductNameSuggestion {
        return typeof value === 'object' && value !== null && 'name' in value && typeof value.name === 'string';
    }

    private buildFieldErrors(): FieldErrors {
        return ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
            errors[field] = this.getControlError(field);
            return errors;
        }, this.createEmptyFieldErrors());
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            name: null,
            productType: null,
            defaultPortionAmount: null,
            baseUnit: null,
            visibility: null,
        };
    }

    private buildVisibilityOptions(): void {
        this.visibilityOptions.set(
            (Object.values(ProductVisibility) as ProductVisibility[]).map(option => ({
                value: option,
                label: this.translateService.instant(`PRODUCT_MANAGE.VISIBILITY_OPTIONS.${option.toUpperCase()}`),
            })),
        );
    }

    private buildUnitOptions(): void {
        this.unitOptions.set(
            (Object.values(MeasurementUnit) as MeasurementUnit[]).map(unit => ({
                value: unit,
                label: this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${MeasurementUnit[unit]}`),
            })),
        );
    }

    private buildProductTypeOptions(): void {
        this.productTypeOptions.set(
            (Object.values(ProductType) as ProductType[]).map(type => ({
                value: type,
                label: this.translateService.instant(`PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.${type.toUpperCase()}`),
            })),
        );
    }

    private getControlError(controlName: ErrorField): string | null {
        const control = this.form()[controlName]();

        if (!control.touched() && !control.dirty()) {
            return null;
        }

        if (!control.invalid()) {
            return null;
        }

        if (control.getError('required') !== undefined) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        const min = getNumberProperty(control.getError('min'), 'min');
        if (min !== undefined) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min,
            });
        }

        return null;
    }

    private refreshTranslatedState(): void {
        this.buildUnitOptions();
        this.buildProductTypeOptions();
        this.buildVisibilityOptions();
        this.fieldErrors.set(this.buildFieldErrors());
    }
}
