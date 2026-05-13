import { describe, expect, it } from 'vitest';

import { ProductType } from '../models/product.data';
import { resolveProductImageUrl } from './product-image.util';

describe('resolveProductImageUrl', () => {
    it('should prefer a non-empty product image url', () => {
        expect(resolveProductImageUrl('https://cdn.example.com/apple.png', ProductType.Fruit)).toBe('https://cdn.example.com/apple.png');
    });

    it('should fall back to product type stub when image url is empty', () => {
        expect(resolveProductImageUrl('   ', ProductType.Meat)).toBe('assets/images/stubs/products/meat.png');
    });

    it('should use unknown stub when product type is missing', () => {
        expect(resolveProductImageUrl(null, null)).toBe('assets/images/stubs/products/other.png');
        expect(resolveProductImageUrl(undefined, undefined)).toBe('assets/images/stubs/products/other.png');
    });
});
