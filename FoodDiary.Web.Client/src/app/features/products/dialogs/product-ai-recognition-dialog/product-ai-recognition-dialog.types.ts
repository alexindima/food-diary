import type { FormControl, FormGroup } from '@angular/forms';

import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import type { MeasurementUnit } from '../../models/product.data';

export type ProductAiDialogData = {
    initialDescription?: string | null;
};

export type ProductAiRecognitionResult = {
    name: string;
    description?: string | null;
    image: ImageSelection | null;
    baseAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
};

export type ProductAiRecognitionFormGroup = FormGroup<{
    name: FormControl<string>;
    portionAmount: FormControl<number>;
    baseUnit: FormControl<MeasurementUnit>;
    caloriesPerBase: FormControl<number>;
    proteinsPerBase: FormControl<number>;
    fatsPerBase: FormControl<number>;
    carbsPerBase: FormControl<number>;
    fiberPerBase: FormControl<number>;
    alcoholPerBase: FormControl<number>;
}>;
