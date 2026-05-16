import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { MealService } from '../../api/meal.service';
import type { Meal } from '../../models/meal.data';
import { buildMealManageDtoFromAiResult } from './ai-meal-result.mapper';

@Injectable({ providedIn: 'root' })
export class AiMealCreateService {
    private readonly mealService = inject(MealService);

    public createFromAiResult(result: AiInputBarResult): Observable<Meal | null> {
        return this.mealService.create(buildMealManageDtoFromAiResult(result));
    }
}
