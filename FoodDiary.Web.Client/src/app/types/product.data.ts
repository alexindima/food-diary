import { PageOf } from './page-of.data';

export interface Product {
    id: string; // Guid
    barcode?: string | null;
    name: string;
    brand?: string | null;
    productType?: ProductType | null;
    category?: string | null;
    description?: string | null;
    comment?: string | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    baseUnit: MeasurementUnit;
    baseAmount: number;
    defaultPortionAmount: number;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
    usageCount: number;
    visibility: ProductVisibility;
    createdAt: Date;
    isOwnedByCurrentUser: boolean;
}

export interface CreateProductRequest {
    barcode?: string | null;
    name: string;
    brand?: string | null;
    productType: ProductType;
    category?: string | null;
    description?: string | null;
    comment?: string | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    baseUnit: MeasurementUnit;
    baseAmount: number;
    defaultPortionAmount: number;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
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

export interface ProductListWithRecent {
    recentItems: Product[];
    allProducts: PageOf<Product>;
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

export enum ProductType {
    Unknown = 'Unknown',
    Meat = 'Meat',
    Fruit = 'Fruit',
    Vegetable = 'Vegetable',
    Cheese = 'Cheese',
    Dairy = 'Dairy',
    Seafood = 'Seafood',
    Grain = 'Grain',
    Beverage = 'Beverage',
    Dessert = 'Dessert',
    Other = 'Other',
}
