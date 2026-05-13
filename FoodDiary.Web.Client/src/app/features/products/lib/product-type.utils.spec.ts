import { describe, expect, it } from 'vitest';

import { ProductType } from '../models/product.data';
import { buildProductTypeTranslationKey, normalizeProductType } from './product-type.utils';

describe('normalizeProductType', () => {
    it('should normalize enum values and case-insensitive strings', () => {
        expect(normalizeProductType(ProductType.Fruit)).toBe(ProductType.Fruit);
        expect(normalizeProductType(' fruit ')).toBe(ProductType.Fruit);
    });

    it('should normalize plural and legacy aliases', () => {
        expect(normalizeProductType('VEGETABLES')).toBe(ProductType.Vegetable);
        expect(normalizeProductType('MILK')).toBe(ProductType.Dairy);
        expect(normalizeProductType('SEAFOODS')).toBe(ProductType.Seafood);
    });

    it('should return null for empty or unknown values', () => {
        expect(normalizeProductType(null)).toBeNull();
        expect(normalizeProductType(undefined)).toBeNull();
        expect(normalizeProductType('')).toBeNull();
        expect(normalizeProductType('not-a-type')).toBeNull();
    });
});

describe('buildProductTypeTranslationKey', () => {
    it('should build translation key from normalized product type', () => {
        expect(buildProductTypeTranslationKey('fruits')).toBe('PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.FRUIT');
    });

    it('should fall back to unknown translation key', () => {
        expect(buildProductTypeTranslationKey('not-a-type')).toBe('PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.UNKNOWN');
    });
});
