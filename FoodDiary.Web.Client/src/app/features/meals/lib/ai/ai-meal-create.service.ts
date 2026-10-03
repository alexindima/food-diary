import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import type { Meal } from '../../../../shared/models/meal.data';
import { MealService } from '../../api/meal.service';
import { buildMealManageDtoFromAiResult } from './ai-meal-result.mapper';

@Service()
export class AiMealCreateService {
    private readonly mealService = inject(MealService);

    public createFromAiResult(result: AiInputBarResult): Observable<Meal> {
        return this.mealService.create(buildMealManageDtoFromAiResult(result));
    }
}
