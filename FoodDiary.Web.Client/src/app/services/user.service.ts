import { Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { catchError, map, Observable, of, tap } from 'rxjs';
import {
    ChangePasswordRequest,
    DashboardLayoutSettings,
    DesiredWaistResponse,
    DesiredWeightResponse,
    UpdateUserDto,
    User,
} from '../types/user.data';

@Injectable({
    providedIn: 'root',
})
export class UserService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.users;
    private readonly userSignal = signal<User | null>(null);
    public readonly user = this.userSignal.asReadonly();

    public clearUser(): void {
        this.userSignal.set(null);
    }

    public getUserCalories(): Observable<number | null> {
        return this.getInfo().pipe(map(user => user?.calories ?? null));
    }

    public getInfo(): Observable<User | null> {
        return this.get<User>('info').pipe(
            tap(user => this.userSignal.set(user ?? null)),
            catchError(error => {
                console.error('Get user info error', error);
                this.userSignal.set(null);
                return of(null);
            }),
        );
    }

    public update(data: UpdateUserDto): Observable<User | null> {
        return this.patch<User>('info', data).pipe(
            tap(user => {
                if (user) {
                    this.userSignal.set(user);
                }
            }),
            catchError(error => {
                console.error('Update user error', error);
                return of(null);
            }),
        );
    }

    public updateDashboardLayout(layout: DashboardLayoutSettings): Observable<User | null> {
        return this.patch<User>('info', { dashboardLayout: layout }).pipe(
            tap(user => {
                if (user) {
                    this.userSignal.set(user);
                }
            }),
            catchError(error => {
                console.error('Update dashboard layout error', error);
                return of(null);
            }),
        );
    }

    public changePassword(request: ChangePasswordRequest): Observable<boolean> {
        return this.patch<void>('password', request).pipe(
            map(() => true),
            catchError(error => {
                console.error('Change password error', error);
                return of(false);
            }),
        );
    }

    public deleteCurrentUser(): Observable<boolean> {
        return this.delete<void>('').pipe(
            tap(() => this.userSignal.set(null)),
            map(() => true),
            catchError(error => {
                console.error('Delete user error', error);
                return of(false);
            }),
        );
    }

    public getDesiredWeight(): Observable<number | null> {
        return this.get<DesiredWeightResponse>('desired-weight').pipe(
            map(response => response.desiredWeight ?? null),
            catchError(error => {
                console.error('Get desired weight error', error);
                return of(null);
            }),
        );
    }

    public updateDesiredWeight(value: number | null): Observable<number | null> {
        return this.put<DesiredWeightResponse>('desired-weight', {
            desiredWeight: value,
        }).pipe(
            map(response => response.desiredWeight ?? null),
            catchError(error => {
                console.error('Update desired weight error', error);
                throw error;
            }),
        );
    }

    public getDesiredWaist(): Observable<number | null> {
        return this.get<DesiredWaistResponse>('desired-waist').pipe(
            map(response => response.desiredWaist ?? null),
            catchError(error => {
                console.error('Get desired waist error', error);
                return of(null);
            }),
        );
    }

    public updateDesiredWaist(value: number | null): Observable<number | null> {
        return this.put<DesiredWaistResponse>('desired-waist', {
            desiredWaist: value,
        }).pipe(
            map(response => response.desiredWaist ?? null),
            catchError(error => {
                console.error('Update desired waist error', error);
                throw error;
            }),
        );
    }
}
