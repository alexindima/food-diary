import type { ProductListFiltersDialogResult } from '../../components/list/product-list-filters-dialog/product-list-filters-dialog.types';
import { type FavoriteProduct, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';

const FAVORITE_GRAM_BASE_AMOUNT = 100;

export type ProductListFilterState = {
    onlyMine: boolean;
    productTypes: ProductType[];
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
};

export type ProductListFilterChanges = {
    productTypes: ProductType[];
    onlyMineChanged: boolean;
    typesChanged: boolean;
    caloriesChanged: boolean;
    imageChanged: boolean;
    hasChanges: boolean;
};

export function excludeRecentProducts(products: readonly Product[], recentProducts: readonly Product[]): Product[] {
    if (recentProducts.length === 0) {
        return [...products];
    }

    const recentIds = new Set(recentProducts.map(product => product.id));
    return products.filter(product => !recentIds.has(product.id));
}

export function getProductListActiveFilterCount(state: ProductListFilterState): number {
    return (
        (state.onlyMine ? 1 : 0) +
        state.productTypes.length +
        (state.caloriesFrom !== null || state.caloriesTo !== null ? 1 : 0) +
        (state.hasImage !== null ? 1 : 0)
    );
}

export function resolveProductListFilterChanges(
    current: ProductListFilterState,
    result: ProductListFiltersDialogResult,
): ProductListFilterChanges {
    const productTypes = [...new Set(result.productTypes)];
    const onlyMineChanged = current.onlyMine !== result.onlyMine;
    const typesChanged = !areProductTypesEqual(current.productTypes, productTypes);
    const caloriesChanged = current.caloriesFrom !== result.caloriesFrom || current.caloriesTo !== result.caloriesTo;
    const imageChanged = current.hasImage !== result.hasImage;

    return {
        productTypes,
        onlyMineChanged,
        typesChanged,
        caloriesChanged,
        imageChanged,
        hasChanges: onlyMineChanged || typesChanged || caloriesChanged || imageChanged,
    };
}

export function buildFavoriteProductSnapshot(favorite: FavoriteProduct): Product {
    const baseUnit = normalizeMeasurementUnit(favorite.baseUnit);
    const name = favorite.name?.trim();

    return {
        id: favorite.productId,
        name: name !== undefined && name.length > 0 ? name : favorite.productName,
        barcode: favorite.barcode ?? null,
        brand: favorite.brand ?? null,
        productType: ProductType.Unknown,
        category: null,
        description: null,
        comment: favorite.comment ?? null,
        imageUrl: favorite.imageUrl ?? null,
        imageAssetId: null,
        baseUnit,
        baseAmount: baseUnit === MeasurementUnit.PCS ? 1 : FAVORITE_GRAM_BASE_AMOUNT,
        defaultPortionAmount: favorite.defaultPortionAmount,
        caloriesPerBase: favorite.caloriesPerBase,
        proteinsPerBase: favorite.proteinsPerBase,
        fatsPerBase: favorite.fatsPerBase,
        carbsPerBase: favorite.carbsPerBase,
        fiberPerBase: favorite.fiberPerBase,
        alcoholPerBase: favorite.alcoholPerBase,
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date(favorite.createdAtUtc),
        isOwnedByCurrentUser: favorite.isOwnedByCurrentUser,
        qualityScore: favorite.qualityScore,
        qualityGrade: favorite.qualityGrade,
        isFavorite: true,
        favoriteProductId: favorite.id,
    };
}

function areProductTypesEqual(left: readonly ProductType[], right: readonly ProductType[]): boolean {
    if (left.length !== right.length) {
        return false;
    }

    const leftSet = new Set(left);
    return right.every(type => leftSet.has(type));
}

function normalizeMeasurementUnit(value: string): MeasurementUnit {
    if (value === 'ML') {
        return MeasurementUnit.ML;
    }

    if (value === 'PCS') {
        return MeasurementUnit.PCS;
    }

    return MeasurementUnit.G;
}
