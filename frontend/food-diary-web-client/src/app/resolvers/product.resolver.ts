import { ResolveFn } from '@angular/router';
import { Product } from '../types/product.data';
import { inject } from '@angular/core';
import { ProductService } from '../services/product.service';
import { catchError, of } from 'rxjs';
import { NavigationService } from '../services/navigation.service';

export const productResolver: ResolveFn<Product | null> = route => {
    const productService = inject(ProductService);
    const navigationService = inject(NavigationService);

    const productId = route.paramMap.get('id');
    if (!productId) {
        navigationService.navigateToProductList();
        return of(null);
    }

    return productService.getById(productId).pipe(
        catchError(() => {
            navigationService.navigateToProductList();
            return of(null);
        }),
    );
};
