import { inject } from '@angular/core';
import type { ResolveFn } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { MealService } from '../api/meal.service';
import type { Meal } from '../models/meal.data';

export const mealResolver: ResolveFn<Meal | null> = route => {
    const mealService = inject(MealService);
    const navigationService = inject(NavigationService);

    const mealId = route.paramMap.get('id')!;

    return mealService.getById(mealId).pipe(
        map(meal => {
            if (meal) {
                return meal;
            }
            void navigationService.navigateToConsumptionListAsync();
            return null;
        }),
        catchError(() => {
            void navigationService.navigateToConsumptionListAsync();
            return of(null);
        }),
    );
};
