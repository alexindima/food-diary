import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import type { MeasurementUnit } from '../../models/product.data';

export type ProductAiDialogData = {
    initialPhotos?: ImageSelection[];
    initialDescription?: string | null;
    hasExistingData?: boolean;
};

export type ProductAiRecognitionResult = {
    brand?: string | null;
    name: string;
    description?: string | null;
    image: ImageSelection | null;
    images?: ImageSelection[];
    baseAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    alcoholPerBase: number | null;
};

export type ProductAiRecognitionFormModel = {
    name: string;
    brand: string;
    portionAmount: number | null;
    baseUnit: MeasurementUnit | null;
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    alcoholPerBase: number | null;
};
