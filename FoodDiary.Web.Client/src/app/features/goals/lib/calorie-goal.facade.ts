import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type { GoalsResponse, UpdateGoalsRequest } from '../../../shared/models/goals.data';
import { GoalsService } from '../api/goals.service';

@Service()
export class CalorieGoalFacade {
    private readonly goalsService = inject(GoalsService);

    public updateGoals(request: UpdateGoalsRequest): Observable<GoalsResponse | null> {
        return this.goalsService.updateGoals(request);
    }
}
