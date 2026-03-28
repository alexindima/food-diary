import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../../models/product.data';
import { ProductFormData } from '../base-product-manage.component';

@Component({
    selector: 'fd-product-basic-info',
    standalone: true,
    templateUrl: './product-basic-info.component.html',
    styleUrls: ['./product-basic-info.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiSelectComponent,
        FdUiCardComponent,
        ImageUploadFieldComponent,
    ],
})
export class ProductBasicInfoComponent {
    public formGroup = input.required<FormGroup<ProductFormData>>();
    public unitOptions = input.required<FdUiSelectOption<MeasurementUnit>[]>();
    public productTypeOptions = input.required<FdUiSelectOption<ProductType>[]>();
    public visibilityOptions = input.required<FdUiSelectOption<ProductVisibility>[]>();
    public getControlError = input.required<(controlName: keyof ProductFormData) => string | null>();

    public openBarcodeScanner = output<void>();
    public openAiRecognition = output<void>();
}
