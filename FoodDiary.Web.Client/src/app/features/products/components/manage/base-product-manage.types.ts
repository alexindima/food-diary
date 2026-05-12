import type { FormGroupControls } from '../../../../shared/lib/common.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import type { MeasurementUnit, ProductType, ProductVisibility } from '../../models/product.data';

export interface ProductFormValues {
    name: string;
    barcode: string | null;
    brand: string | null;
    productType: ProductType;
    description: string | null;
    comment: string | null;
    imageUrl: ImageSelection | null;
    baseAmount: number;
    defaultPortionAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    alcoholPerBase: number | null;
    visibility: ProductVisibility;
    usdaFdcId: number | null;
}

export type NutritionMode = 'base' | 'portion';

export type ProductFormData = FormGroupControls<ProductFormValues>;
