import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiAutocompleteComponent, type FdUiAutocompleteOption } from 'fd-ui-kit/autocomplete/fd-ui-autocomplete.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { getNumberProperty } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../../models/product.data';
import type { ProductFormData } from '../product-manage-lib/product-manage-form.types';
import type { ProductNameAutocompleteOption, ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';

const ERROR_FIELDS = ['name', 'productType', 'defaultPortionAmount', 'baseUnit', 'visibility'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-product-basic-info',
    standalone: true,
    templateUrl: './product-basic-info.component.html',
    styleUrls: ['./product-basic-info.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
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

    public readonly formGroup = input.required<FormGroup<ProductFormData>>();
    public readonly nameOptions = input.required<ProductNameAutocompleteOption[]>();
    public readonly isNameSearchLoading = input.required<boolean>();
    public readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());
    public readonly unitOptions = signal<Array<FdUiSelectOption<MeasurementUnit>>>([]);
    public readonly productTypeOptions = signal<Array<FdUiSelectOption<ProductType>>>([]);
    public readonly visibilityOptions = signal<Array<FdUiSelectOption<ProductVisibility>>>([]);

    public readonly openBarcodeScanner = output();
    public readonly openAiRecognition = output();
    public readonly nameQueryChange = output<string>();
    public readonly nameSuggestionSelected = output<ProductNameSuggestion>();

    public readonly displayNameValue = (value: string | null): string => value ?? '';

    private readonly errorSync = effect(onCleanup => {
        const form = this.formGroup();
        const formEvents = (form as { events?: Observable<unknown> }).events ?? EMPTY;
        const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
        const refresh = (): void => {
            this.fieldErrors.set(this.buildFieldErrors());
        };

        refresh();
        const subscription = merge(formEvents, form.statusChanges, form.valueChanges, languageChanges).subscribe(() => {
            refresh();
        });
        onCleanup(() => {
            subscription.unsubscribe();
        });
    });

    private readonly optionSync = effect(onCleanup => {
        const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
        const refresh = (): void => {
            this.buildUnitOptions();
            this.buildProductTypeOptions();
            this.buildVisibilityOptions();
        };

        refresh();
        const subscription = languageChanges.subscribe(() => {
            refresh();
        });
        onCleanup(() => {
            subscription.unsubscribe();
        });
    });

    public onNameOptionSelected(option: FdUiAutocompleteOption<string>): void {
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
        const control = this.formGroup().controls[controlName];

        if (!control.touched && !control.dirty) {
            return null;
        }

        const errors = control.errors;

        if (errors === null) {
            return null;
        }

        if (errors['required'] !== undefined) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        const min = getNumberProperty(errors['min'], 'min');
        if (min !== undefined) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min,
            });
        }

        return null;
    }
}
