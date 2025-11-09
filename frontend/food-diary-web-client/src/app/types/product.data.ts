export interface Product {
    id: string; // Guid
    barcode?: string | null;
    name: string;
    brand?: string | null;
    category?: string | null;
    description?: string | null;
    imageUrl?: string | null;
    baseUnit: MeasurementUnit;
    baseAmount: number;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    usageCount: number;
    visibility: ProductVisibility;
    createdAt: Date;
    isOwnedByCurrentUser: boolean;
}

export interface CreateProductRequest {
    barcode?: string | null;
    name: string;
    brand?: string | null;
    category?: string | null;
    description?: string | null;
    imageUrl?: string | null;
    baseUnit: MeasurementUnit;
    baseAmount: number;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    visibility: ProductVisibility;
}

export class ProductFilters {
    public search?: string;

    public constructor(search: string | null) {
        if (search) {
            this.search = search;
        }
    }
}

export enum MeasurementUnit {
    G = 'G',
    ML = 'ML',
    PCS = 'PCS',
}

export enum ProductVisibility {
    Private = 'Private',
    Public = 'Public',
}
