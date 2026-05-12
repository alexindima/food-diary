import type { Product } from '../../models/product.data';

export interface ProductCardViewModel {
    product: Product;
    imageUrl: string | undefined;
}
