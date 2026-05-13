import type { ProductType } from '../../../models/product.data';

export type ProductListVisibilityFilter = 'all' | 'mine';

export type ProductListFiltersDialogData = {
    onlyMine: boolean;
    productTypes: ProductType[];
};

export type ProductListFiltersDialogResult = {
    onlyMine: boolean;
    productTypes: ProductType[];
};
