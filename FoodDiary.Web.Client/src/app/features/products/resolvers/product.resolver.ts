import { inject } from '@angular/core';
import type { ResolveFn } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { ProductService } from '../api/product.service';
import type { Product } from '../models/product.data';

export const productResolver: ResolveFn<Product | null> = route => {
    const productService = inject(ProductService);
    const navigationService = inject(NavigationService);

    const productId = route.paramMap.get('id');
    if (productId === null || productId.trim().length === 0) {
        void navigationService.navigateToProductListAsync();
        return of(null);
    }

    return productService.getById(productId).pipe(
        map(product => {
            if (product === null || !product.isOwnedByCurrentUser || product.usageCount > 0) {
                void navigationService.navigateToProductListAsync();
                return null;
            }

            return product;
        }),
        catchError(() => {
            void navigationService.navigateToProductListAsync();
            return of(null);
        }),
    );
};
