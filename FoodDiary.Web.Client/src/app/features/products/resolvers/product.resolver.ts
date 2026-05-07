import { inject } from '@angular/core';
import { type ResolveFn } from '@angular/router';
import { catchError, of } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { ProductService } from '../api/product.service';
import { type Product } from '../models/product.data';

export const productResolver: ResolveFn<Product | null> = route => {
    const productService = inject(ProductService);
    const navigationService = inject(NavigationService);

    const productId = route.paramMap.get('id');
    if (!productId) {
        void navigationService.navigateToProductListAsync();
        return of(null);
    }

    return productService.getById(productId).pipe(
        catchError(() => {
            void navigationService.navigateToProductListAsync();
            return of(null);
        }),
    );
};
