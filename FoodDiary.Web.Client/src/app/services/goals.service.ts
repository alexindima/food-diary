import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { catchError, Observable, of } from 'rxjs';
import { GoalsResponse, UpdateGoalsRequest } from '../types/goals.data';

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
