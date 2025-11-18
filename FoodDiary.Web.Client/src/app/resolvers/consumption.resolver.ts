import { ResolveFn } from '@angular/router';
import { inject } from '@angular/core';
import { catchError, map, of } from 'rxjs';
import { NavigationService } from '../services/navigation.service';
import { ConsumptionService } from '../services/consumption.service';
import { Consumption } from '../types/consumption.data';

export const consumptionResolver: ResolveFn<Consumption | null> = route => {
    const foodService = inject(ConsumptionService);
    const navigationService = inject(NavigationService);

    const consumptionId = route.paramMap.get('id')!;

    return foodService.getById(+consumptionId).pipe(
        map(consumption => {
            if (consumption) {
                return consumption;
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
