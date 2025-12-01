import { ProductType } from '../types/product.data';

const PRODUCT_STUBS: Record<ProductType, string> = {
    [ProductType.Unknown]: 'assets/images/stubs/products/other.png',
    [ProductType.Meat]: 'assets/images/stubs/products/meat.png',
    [ProductType.Fruit]: 'assets/images/stubs/products/fruit.png',
    [ProductType.Vegetable]: 'assets/images/stubs/products/vegetable.png',
    [ProductType.Cheese]: 'assets/images/stubs/products/cheese.png',
    [ProductType.Dairy]: 'assets/images/stubs/products/dairy.png',
    [ProductType.Seafood]: 'assets/images/stubs/products/seafood.png',
    [ProductType.Grain]: 'assets/images/stubs/products/grain.png',
    [ProductType.Beverage]: 'assets/images/stubs/products/beverage.png',
    [ProductType.Dessert]: 'assets/images/stubs/products/dessert.png',
    [ProductType.Other]: 'assets/images/stubs/products/other.png',
};

export function resolveProductImageUrl(imageUrl: string | null | undefined, type: ProductType | null | undefined): string | undefined {
    if (imageUrl && imageUrl.trim().length > 0) {
        return imageUrl;
    }
    if (!type) {
        return PRODUCT_STUBS[ProductType.Unknown];
    }
    return PRODUCT_STUBS[type] ?? PRODUCT_STUBS[ProductType.Unknown];
}
