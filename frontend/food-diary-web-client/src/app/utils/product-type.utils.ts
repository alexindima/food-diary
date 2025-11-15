import { ProductType } from '../types/product.data';

const PRODUCT_TYPE_ALIASES: Record<string, ProductType> = {
    VEGETABLES: ProductType.Vegetable,
    FRUITS: ProductType.Fruit,
    MEATS: ProductType.Meat,
    CHEESES: ProductType.Cheese,
    DAIRY: ProductType.Dairy,
    MILK: ProductType.Dairy,
    SEAFOODS: ProductType.Seafood,
    GRAINS: ProductType.Grain,
    BEVERAGES: ProductType.Beverage,
    DESSERTS: ProductType.Dessert,
    OTHERS: ProductType.Other,
};

export function normalizeProductType(
    value?: ProductType | string | null,
): ProductType | null {
    if (!value) {
        return null;
    }

    const upper = value.toString().trim().toUpperCase();
    const alias = PRODUCT_TYPE_ALIASES[upper];
    if (alias) {
        return alias;
    }

    const match = Object.values(ProductType).find(
        type => type.toUpperCase() === upper,
    );

    return match ?? null;
}

export function buildProductTypeTranslationKey(value?: ProductType | string | null): string {
    const normalized = normalizeProductType(value) ?? ProductType.Unknown;
    return `PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.${normalized.toUpperCase()}`;
}
