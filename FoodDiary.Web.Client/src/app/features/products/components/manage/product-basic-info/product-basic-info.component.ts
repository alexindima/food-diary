import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAutocompleteComponent, FdUiAutocompleteOption } from 'fd-ui-kit/autocomplete/fd-ui-autocomplete.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import { MeasurementUnit, ProductSearchSuggestion, ProductType, ProductVisibility } from '../../../models/product.data';
import { ProductFormData } from '../base-product-manage.component';

export type ProductNameSuggestion = ProductSearchSuggestion;

export type ProductNameAutocompleteOption = FdUiAutocompleteOption<string> & { data: ProductNameSuggestion };

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
    public readonly formGroup = input.required<FormGroup<ProductFormData>>();
    public readonly unitOptions = input.required<FdUiSelectOption<MeasurementUnit>[]>();
    public readonly productTypeOptions = input.required<FdUiSelectOption<ProductType>[]>();
    public readonly visibilityOptions = input.required<FdUiSelectOption<ProductVisibility>[]>();
    public readonly nameOptions = input.required<ProductNameAutocompleteOption[]>();
    public readonly isNameSearchLoading = input.required<boolean>();
    public readonly getControlError = input.required<(controlName: keyof ProductFormData) => string | null>();

    public openBarcodeScanner = output<void>();
    public openAiRecognition = output<void>();
    public nameQueryChange = output<string>();
    public nameSuggestionSelected = output<ProductNameSuggestion>();

    public readonly displayNameValue = (value: string | null): string => value ?? '';

    public onNameOptionSelected(option: FdUiAutocompleteOption<string>): void {
        const suggestion = option.data as ProductNameSuggestion | undefined;
        if (suggestion) {
            this.nameSuggestionSelected.emit(suggestion);
        }
    }
}
