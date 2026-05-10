import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import type { WeeklyCheckInData, WeekSummary, WeekTrend } from '../models/weekly-check-in.data';

@Injectable({
    providedIn: 'root',
})
export class WeeklyCheckInService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.weeklyCheckIn;

    public getData(): Observable<WeeklyCheckInData> {
        return super.get<WeeklyCheckInData>('').pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get weekly check-in error', error, {
                    thisWeek: createEmptySummary(),
                    lastWeek: createEmptySummary(),
                    trends: createEmptyTrend(),
                    suggestions: [],
                }),
            ),
        );
    }
}

function createEmptySummary(): WeekSummary {
    return {
        totalCalories: 0,
        avgDailyCalories: 0,
        avgProteins: 0,
        avgFats: 0,
        avgCarbs: 0,
        mealsLogged: 0,
        daysLogged: 0,
        weightStart: null,
        weightEnd: null,
        waistStart: null,
        waistEnd: null,
        totalHydrationMl: 0,
        avgDailyHydrationMl: 0,
    };
}

function createEmptyTrend(): WeekTrend {
    return {
        calorieChange: 0,
        proteinChange: 0,
        fatChange: 0,
        carbChange: 0,
        weightChange: null,
        waistChange: null,
        hydrationChange: 0,
        mealsLoggedChange: 0,
    };
}
