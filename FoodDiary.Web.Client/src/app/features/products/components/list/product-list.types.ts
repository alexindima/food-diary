import type { Product } from '../../models/product.data';

export type ProductCardViewModel = {
    product: Product;
    imageUrl: string | undefined;
};
