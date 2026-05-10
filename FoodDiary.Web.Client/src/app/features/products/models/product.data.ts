import type { PageOf } from '../../../shared/models/page-of.data';

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
    qualityScore: number;
    qualityGrade: QualityGrade;
    usdaFdcId?: number | null;
    isFavorite?: boolean;
    favoriteProductId?: string | null;
}

export type ProductSearchSuggestionSource = 'openFoodFacts' | 'usda';

export interface ProductSearchSuggestion {
    source: ProductSearchSuggestionSource;
    name: string;
    brand?: string | null;
    category?: string | null;
    barcode?: string | null;
    usdaFdcId?: number | null;
    imageUrl?: string | null;
    caloriesPer100G?: number | null;
    proteinsPer100G?: number | null;
    fatsPer100G?: number | null;
    carbsPer100G?: number | null;
    fiberPer100G?: number | null;
}

export type QualityGrade = 'green' | 'yellow' | 'red';

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

export interface UpdateProductRequest extends Partial<CreateProductRequest> {
    clearBarcode?: boolean;
    clearBrand?: boolean;
    clearCategory?: boolean;
    clearDescription?: boolean;
    clearComment?: boolean;
    clearImageUrl?: boolean;
    clearImageAssetId?: boolean;
}

export class ProductFilters {
    public search?: string;
    public productTypes?: ProductType[];

    public constructor(search: string | null, productTypes?: ProductType[]) {
        if (search) {
            this.search = search;
        }

        if (productTypes && productTypes.length > 0) {
            this.productTypes = productTypes;
        }
    }
}

export interface ProductOverview {
    recentItems: Product[];
    allProducts: PageOf<Product>;
    favoriteItems: FavoriteProduct[];
    favoriteTotalCount: number;
}

export interface FavoriteProduct {
    id: string;
    productId: string;
    name?: string | null;
    createdAtUtc: string;
    productName: string;
    brand?: string | null;
    imageUrl?: string | null;
    caloriesPerBase: number;
    baseUnit: string;
    defaultPortionAmount: number;
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
