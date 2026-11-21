import type { ProductType } from '../../../../shared/models/product.data';
import type { ProductListFiltersDialogResult } from '../../components/list/product-list-filters-dialog/product-list-filters-dialog.types';

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

function areProductTypesEqual(left: readonly ProductType[], right: readonly ProductType[]): boolean {
    if (left.length !== right.length) {
        return false;
    }

    const leftSet = new Set(left);
    return right.every(type => leftSet.has(type));
}
