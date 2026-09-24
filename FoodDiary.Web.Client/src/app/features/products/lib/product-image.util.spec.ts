import { describe, expect, it } from 'vitest';

import { ProductType } from '../models/product.data';
import { resolveProductImageUrl } from './product-image.util';

describe('resolveProductImageUrl', () => {
    it('should prefer a non-empty product image url', () => {
        expect(resolveProductImageUrl('https://cdn.example.com/apple.png', ProductType.Fruit)).toBe('https://cdn.example.com/apple.png');
    });

    it('should leave the icon placeholder when image url is empty', () => {
        expect(resolveProductImageUrl('   ', ProductType.Meat)).toBeUndefined();
    });

    it('should leave the icon placeholder when product type is missing', () => {
        expect(resolveProductImageUrl(null, null)).toBeUndefined();
        expect(resolveProductImageUrl(void 0, void 0)).toBeUndefined();
    });
});
