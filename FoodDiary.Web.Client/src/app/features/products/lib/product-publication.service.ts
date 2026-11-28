import { inject, Service } from '@angular/core';
import { defaultIfEmpty, firstValueFrom, forkJoin, NEVER, type Observable, takeUntil } from 'rxjs';

import { type Product, ProductVisibility } from '../../../shared/models/product.data';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { ProductService } from '../api/product.service';

@Service()
export class ProductPublicationService {
    private readonly products = inject(ProductService);

    public loadAsync(ids: readonly string[]): Promise<Product[]>;
    public loadAsync(ids: readonly string[], cancelLookup: Observable<unknown> | undefined): Promise<Product[] | null>;
    public async loadAsync(ids: readonly string[], cancelLookup: Observable<unknown> = NEVER): Promise<Product[] | null> {
        const reads = [...new Set(ids)].map(id => this.products.getById(entityId<'product'>(id)).pipe(defaultIfEmpty(null)));
        if (reads.length === 0) {
            return [];
        }
        const products = await firstValueFrom(forkJoin(reads).pipe(takeUntil(cancelLookup)), { defaultValue: null });
        if (products === null) {
            return null;
        }
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
