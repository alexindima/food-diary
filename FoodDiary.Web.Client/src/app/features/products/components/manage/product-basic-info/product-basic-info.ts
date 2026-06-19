import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiAutocompleteComponent, type FdUiAutocompleteOption } from 'fd-ui-kit/autocomplete/fd-ui-autocomplete';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import {
    getProductMaxAmountForUnit,
    PRODUCT_BARCODE_MAX_LENGTH,
    PRODUCT_BRAND_MAX_LENGTH,
    PRODUCT_COMMENT_MAX_LENGTH,
    PRODUCT_DESCRIPTION_MAX_LENGTH,
    PRODUCT_NAME_MAX_LENGTH,
} from '../../../lib/product-manage.constants';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../../models/product.data';
import type { ProductFormValues } from '../product-manage-lib/product-manage-form.types';
import type { ProductNameAutocompleteOption, ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';

const ERROR_FIELDS = [
    'name',
    'barcode',
    'brand',
    'description',
    'productType',
    'defaultPortionAmount',
    'baseUnit',
    'visibility',
    'comment',
] as const;
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
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly languageVersion = signal(0);

    public readonly form = input.required<FieldTree<ProductFormValues>>();
    public readonly nameOptions = input.required<ProductNameAutocompleteOption[]>();
    public readonly isNameSearchLoading = input.required<boolean>();
    protected readonly fieldErrors = computed<FieldErrors>(() => {
        this.languageVersion();

        return this.buildFieldErrors();
    });
    protected readonly unitOptions = computed<Array<FdUiSelectOption<MeasurementUnit>>>(() => {
        this.languageVersion();

        return (Object.values(MeasurementUnit) as MeasurementUnit[]).map(unit => ({
            value: unit,
            label: this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${MeasurementUnit[unit]}`),
        }));
    });
    protected readonly productTypeOptions = computed<Array<FdUiSelectOption<ProductType>>>(() => {
        this.languageVersion();

        return (Object.values(ProductType) as ProductType[]).map(type => ({
            value: type,
            label: this.translateService.instant(`PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.${type.toUpperCase()}`),
        }));
    });
    protected readonly visibilityOptions = computed<Array<FdUiSelectOption<ProductVisibility>>>(() => {
        this.languageVersion();

        return (Object.values(ProductVisibility) as ProductVisibility[]).map(option => ({
            value: option,
            label: this.translateService.instant(`PRODUCT_MANAGE.VISIBILITY_OPTIONS.${option.toUpperCase()}`),
        }));
    });

    public readonly openBarcodeScanner = output();
    public readonly openAiRecognition = output();
    public readonly nameQueryChange = output<string>();
    public readonly nameSuggestionSelected = output<ProductNameSuggestion>();

    protected readonly displayNameValue = (value: string | null): string => value ?? '';
    protected readonly nameMaxLength = PRODUCT_NAME_MAX_LENGTH;
    protected readonly barcodeMaxLength = PRODUCT_BARCODE_MAX_LENGTH;
    protected readonly brandMaxLength = PRODUCT_BRAND_MAX_LENGTH;
    protected readonly descriptionMaxLength = PRODUCT_DESCRIPTION_MAX_LENGTH;
    protected readonly commentMaxLength = PRODUCT_COMMENT_MAX_LENGTH;
    protected readonly maxAmount = computed(() => getProductMaxAmountForUnit(this.form().baseUnit().value()));

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
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
            barcode: null,
            brand: null,
            description: null,
            productType: null,
            defaultPortionAmount: null,
            baseUnit: null,
            visibility: null,
            comment: null,
        };
    }

    private getControlError(controlName: ErrorField): string | null {
        return resolveSignalFormFieldError(this.form()[controlName], this.validationErrors, this.translateService);
    }
}
