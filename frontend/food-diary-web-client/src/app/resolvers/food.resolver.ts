import { ResolveFn } from '@angular/router';
import { Product } from '../types/product.data';
import { inject } from '@angular/core';
import { ProductService } from '../services/product.service';
import { catchError, of } from 'rxjs';
import { NavigationService } from '../services/navigation.service';

export const foodResolver: ResolveFn<Product | null> = route => {
    const productService = inject(ProductService);
    const navigationService = inject(NavigationService);

    const productId = route.paramMap.get('id');
    if (!productId) {
        navigationService.navigateToFoodList();
        return of(null);
    }

    return productService.getById(productId).pipe(
        catchError(() => {
            navigationService.navigateToFoodList();
            return of(null);
        }),
    );
};
