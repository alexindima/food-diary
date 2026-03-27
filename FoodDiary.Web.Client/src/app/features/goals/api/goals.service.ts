import { Injectable } from '@angular/core';
import { catchError, Observable, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { GoalsResponse, UpdateGoalsRequest } from '../models/goals.data';

@Injectable({
    providedIn: 'root',
})
export class GoalsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.goals;

    public getGoals(): Observable<GoalsResponse | null> {
        return this.get<GoalsResponse>('').pipe(
            catchError(error => {
                console.error('Get goals error', error);
                return of(null);
            }),
        );
    }

    public updateGoals(request: UpdateGoalsRequest): Observable<GoalsResponse | null> {
        return this.patch<GoalsResponse>('', request).pipe(
            catchError(error => {
                console.error('Update goals error', error);
                return of(null);
            }),
        );
    }
}
