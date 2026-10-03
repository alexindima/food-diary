import type { Product } from '../../../../shared/models/product.data';

export type ProductSelectItemViewModel = {
    product: Product;
    imageUrl: string | undefined;
};
