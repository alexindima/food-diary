import type { ProductType } from '../models/product.data';

export function resolveProductImageUrl(imageUrl: string | null | undefined, _type: ProductType | null | undefined): string | undefined {
    const url = imageUrl?.trim();
    return url !== undefined && url.length > 0 ? url : undefined;
}
