import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiAutocompleteComponent, type FdUiAutocompleteOption } from 'fd-ui-kit/autocomplete/fd-ui-autocomplete.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import type { MeasurementUnit, ProductSearchSuggestion, ProductType, ProductVisibility } from '../../../models/product.data';
import type { ProductFormData } from '../base-product-manage.component';

export type ProductNameSuggestion = ProductSearchSuggestion;

export type ProductNameAutocompleteOption = FdUiAutocompleteOption<string> & { data: ProductNameSuggestion };

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
    public readonly unitOptions = input.required<Array<FdUiSelectOption<MeasurementUnit>>>();
    public readonly productTypeOptions = input.required<Array<FdUiSelectOption<ProductType>>>();
    public readonly visibilityOptions = input.required<Array<FdUiSelectOption<ProductVisibility>>>();
    public readonly nameOptions = input.required<ProductNameAutocompleteOption[]>();
    public readonly isNameSearchLoading = input.required<boolean>();
    public readonly getControlError = input.required<(controlName: keyof ProductFormData) => string | null>();
    public readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());

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

    public onNameOptionSelected(option: FdUiAutocompleteOption<string>): void {
        if (this.isProductNameSuggestion(option.data)) {
            this.nameSuggestionSelected.emit(option.data);
        }
    }

    private isProductNameSuggestion(value: unknown): value is ProductNameSuggestion {
        return typeof value === 'object' && value !== null && 'name' in value && typeof value.name === 'string';
    }

    private buildFieldErrors(): FieldErrors {
        const getControlError = this.getControlError();
        return ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
            errors[field] = getControlError(field);
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
}
