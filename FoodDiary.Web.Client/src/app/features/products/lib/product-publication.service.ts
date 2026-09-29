import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ProductService } from '../api/product.service';
import { type Product, ProductVisibility } from '../models/product.data';

@Service()
export class ProductPublicationService {
    private readonly products = inject(ProductService);

    public async loadAsync(ids: readonly string[]): Promise<Product[]> {
        const products = await Promise.all([...new Set(ids)].map(async id => firstValueFrom(this.products.getById(id))));
        if (products.includes(null)) {
            throw new Error('Cannot verify ingredient visibility');
        }
        return products.filter((product): product is Product => product !== null);
    }

    public async publishAsync(products: readonly Product[]): Promise<void> {
        for (const product of products) {
            if (!product.isOwnedByCurrentUser) {
                throw new Error('Cannot publish another author’s product');
            }
            await firstValueFrom(this.products.update(product.id, { visibility: ProductVisibility.Public }));
        }
    }
}
