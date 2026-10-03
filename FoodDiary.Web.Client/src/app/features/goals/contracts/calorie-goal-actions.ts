import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { GoalsResponse } from '../../../shared/models/goals.data';

export type CalorieGoalActions = {
    updateGoals: (request: { dailyCalorieTarget: number }) => Observable<GoalsResponse | null>;
};

export const CALORIE_GOAL_ACTIONS = new InjectionToken<CalorieGoalActions>('CalorieGoalActions');
