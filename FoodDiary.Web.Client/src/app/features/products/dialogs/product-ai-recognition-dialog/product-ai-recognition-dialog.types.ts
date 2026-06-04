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

export type ProductAiRecognitionFormModel = {
    name: string;
    portionAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
};
