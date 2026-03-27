import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { MealService } from '../api/meal.service';
import { Meal } from '../models/meal.data';

export const mealResolver: ResolveFn<Meal | null> = route => {
    const mealService = inject(MealService);
    const navigationService = inject(NavigationService);

    const mealId = route.paramMap.get('id')!;

    return mealService.getById(mealId).pipe(
        map(meal => {
            if (meal) {
                return meal;
            }
            void navigationService.navigateToConsumptionList();
            return null;
        }),
        catchError(() => {
            void navigationService.navigateToConsumptionList();
            return of(null);
        }),
    );
};
