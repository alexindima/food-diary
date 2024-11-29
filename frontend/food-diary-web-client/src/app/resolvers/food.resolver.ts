import { ResolveFn } from '@angular/router';
import { Food } from '../types/food.data';
import { inject } from '@angular/core';
import { FoodService } from '../services/food.service';
import { catchError, map, of } from 'rxjs';
import { NavigationService } from '../services/navigation.service';

export const foodResolver: ResolveFn<Food | null> = route => {
    const foodService = inject(FoodService);
    const navigationService = inject(NavigationService);

    const foodId = route.paramMap.get('id')!;

    return foodService.getById(+foodId).pipe(
        map(response => {
            if (response.status === 'success' && response.data) {
                return response.data;
            }
            navigationService.navigateToFoodList();
            return null;
        }),
        catchError(() => {
            navigationService.navigateToFoodList();
            return of(null);
        }),
    );
};
