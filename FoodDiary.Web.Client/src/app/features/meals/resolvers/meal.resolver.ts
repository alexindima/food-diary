import { inject } from '@angular/core';
import type { ResolveFn } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import type { Meal } from '../../../shared/models/meal.data';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { MealService } from '../api/meal.service';

export const mealResolver: ResolveFn<Meal | null> = route => {
    const mealService = inject(MealService);
    const navigationService = inject(NavigationService);

    const mealId = route.paramMap.get('id');
    if (mealId === null || mealId.trim().length === 0) {
        void navigationService.navigateToMealListAsync();
        return of(null);
    }

    return mealService.getById(entityId<'meal'>(mealId)).pipe(
        map(meal => {
            if (meal !== null) {
                return meal;
            }
            void navigationService.navigateToMealListAsync();
            return null;
        }),
        catchError(() => {
            void navigationService.navigateToMealListAsync();
            return of(null);
        }),
    );
};
