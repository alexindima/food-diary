import type { PageOf } from '../../../shared/models/page-of.data';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';

export type Product = {
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
};

export type ProductSearchSuggestionSource = 'openFoodFacts' | 'usda';

export type ProductSearchSuggestion = {
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
};

export type CreateProductRequest = {
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
};

export type UpdateProductRequest = {
    clearBarcode?: boolean;
    clearBrand?: boolean;
    clearCategory?: boolean;
    clearDescription?: boolean;
    clearComment?: boolean;
    clearImageUrl?: boolean;
    clearImageAssetId?: boolean;
} & Partial<CreateProductRequest>;

export class ProductFilters {
    public search?: string;
    public productTypes?: ProductType[];
    public caloriesFrom?: number;
    public caloriesTo?: number;
    public hasImage?: boolean;

    public constructor(filters: ProductFilterValues) {
        if (filters.search !== null && filters.search.trim().length > 0) {
            this.search = filters.search;
        }

        if (filters.productTypes !== undefined && filters.productTypes.length > 0) {
            this.productTypes = filters.productTypes;
        }

        this.caloriesFrom = normalizeProductNumberFilter(filters.caloriesFrom);
        this.caloriesTo = normalizeProductNumberFilter(filters.caloriesTo);

        if (filters.hasImage !== null && filters.hasImage !== undefined) {
            this.hasImage = filters.hasImage;
        }
    }
}

export type ProductFilterValues = {
    search: string | null;
    productTypes?: ProductType[];
    caloriesFrom?: number | null;
    caloriesTo?: number | null;
    hasImage?: boolean | null;
};

function normalizeProductNumberFilter(value: number | null | undefined): number | undefined {
    return value !== null && value !== undefined && value >= 0 ? value : undefined;
}

export type ProductOverview = {
    recentItems: Product[];
    allProducts: PageOf<Product>;
    favoriteItems: FavoriteProduct[];
    favoriteTotalCount: number;
};

export type FavoriteProduct = {
    id: string;
    productId: string;
    name?: string | null;
    createdAtUtc: string;
    productName: string;
    brand?: string | null;
    barcode?: string | null;
    comment?: string | null;
    imageUrl?: string | null;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
    qualityScore: number;
    qualityGrade: QualityGrade;
    isOwnedByCurrentUser: boolean;
    baseUnit: string;
    preferredPortionAmount: number;
    defaultPortionAmount: number;
};

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
