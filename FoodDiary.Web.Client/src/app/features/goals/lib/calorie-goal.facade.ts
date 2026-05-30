import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { GoalsService } from '../api/goals.service';
import type { GoalsResponse, UpdateGoalsRequest } from '../models/goals.data';

@Injectable({ providedIn: 'root' })
export class CalorieGoalFacade {
    private readonly goalsService = inject(GoalsService);

    public updateGoals(request: UpdateGoalsRequest): Observable<GoalsResponse | null> {
        return this.goalsService.updateGoals(request);
    }
}
