import type { Product } from '../models/product.data';

export interface ProductSelectItemViewModel {
    product: Product;
    imageUrl: string | undefined;
}
