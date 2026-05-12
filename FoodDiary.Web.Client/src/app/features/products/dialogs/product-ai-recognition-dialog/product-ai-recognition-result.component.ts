import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import type { FoodNutritionResponse } from '../../../../shared/models/ai.data';
import type { MeasurementUnit } from '../../models/product.data';
import type { ProductAiRecognitionFormGroup } from './product-ai-recognition-dialog.types';

@Component({
    selector: 'fd-product-ai-recognition-result',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './product-ai-recognition-result.component.html',
    styleUrl: './product-ai-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductAiRecognitionResultComponent {
    public readonly form = input.required<ProductAiRecognitionFormGroup>();
    public readonly nutrition = input.required<FoodNutritionResponse>();
    public readonly itemNames = input.required<readonly string[]>();
    public readonly hasMultipleItems = input.required<boolean>();
    public readonly unitOptions = input.required<Array<FdUiSelectOption<MeasurementUnit>>>();
}
