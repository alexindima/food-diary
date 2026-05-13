import type { Product } from '../../models/product.data';

export type ProductSelectItemViewModel = {
    product: Product;
    imageUrl: string | undefined;
};
