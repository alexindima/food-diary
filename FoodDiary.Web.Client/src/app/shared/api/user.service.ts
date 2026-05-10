import { HttpContext } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, map, type Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import type { DietologistRelationship } from '../../features/dietologist/models/dietologist.data';
import { ApiService } from '../../services/api.service';
import type { NotificationPreferences, WebPushSubscriptionItem } from '../../services/notification.service';
import { fallbackApiError, rethrowApiError } from '../lib/api-error.utils';
import {
    type ChangePasswordRequest,
    type DashboardLayoutSettings,
    type DesiredWaistResponse,
    type DesiredWeightResponse,
    type SetPasswordRequest,
    UpdateUserAppearanceDto,
    type UpdateUserDto,
    type User,
} from '../models/user.data';

export interface UserProfileOverview {
    user: User;
    notificationPreferences: NotificationPreferences;
    webPushSubscriptions: WebPushSubscriptionItem[];
    dietologistRelationship: DietologistRelationship | null;
}

@Injectable({
    providedIn: 'root',
})
export class UserService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.users;
    private readonly silentLoadingContext = new HttpContext().set(SKIP_GLOBAL_LOADING, true);
    private readonly userSignal = signal<User | null>(null);
    public readonly user = this.userSignal.asReadonly();

    public clearUser(): void {
        this.userSignal.set(null);
    }

    public getUserCalories(): Observable<number | null> {
        return this.getInfo().pipe(map(user => user?.calories ?? null));
    }

    public getOverview(): Observable<UserProfileOverview | null> {
        return this.get<UserProfileOverview>('overview').pipe(
            tap(overview => {
                this.userSignal.set(overview.user);
            }),
            catchError(error => {
                this.userSignal.set(null);
                return fallbackApiError('Get user overview error', error, null);
            }),
        );
    }

    public getInfo(): Observable<User | null> {
        return this.get<User>('info').pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError(error => {
                this.userSignal.set(null);
                return fallbackApiError('Get user info error', error, null);
            }),
        );
    }

    public getInfoSilently(): Observable<User | null> {
        return this.get<User>('info', undefined, undefined, this.silentLoadingContext).pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError(error => {
                this.userSignal.set(null);
                return fallbackApiError('Get user info error', error, null);
            }),
        );
    }

    public update(data: UpdateUserDto): Observable<User | null> {
        return this.patch<User>('info', data).pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError(error => fallbackApiError('Update user error', error, null)),
        );
    }

    public updateTheme(theme: string): Observable<User | null> {
        return this.updateAppearance(new UpdateUserAppearanceDto({ theme })).pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError(error => fallbackApiError('Update user theme error', error, null)),
        );
    }

    public updateAppearance(data: UpdateUserAppearanceDto): Observable<User | null> {
        return this.patch<User>('preferences/appearance', data).pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError(error => fallbackApiError('Update user appearance error', error, null)),
        );
    }

    public updateDashboardLayout(layout: DashboardLayoutSettings): Observable<User | null> {
        return this.patch<User>('info', { dashboardLayout: layout }).pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError(error => fallbackApiError('Update dashboard layout error', error, null)),
        );
    }

    public changePassword(request: ChangePasswordRequest): Observable<boolean> {
        return this.patch<void>('password', request).pipe(
            map(() => true),
            catchError(error => fallbackApiError('Change password error', error, false)),
        );
    }

    public setPassword(request: SetPasswordRequest): Observable<boolean> {
        return this.patch<void>('password/set', request).pipe(
            tap(() => {
                const current = this.userSignal();
                if (current !== null) {
                    this.userSignal.set({ ...current, hasPassword: true });
                }
            }),
            map(() => true),
            catchError(error => fallbackApiError('Set password error', error, false)),
        );
    }

    public acceptAiConsent(): Observable<void> {
        return this.post<void>('ai-consent', {}).pipe(
            tap(() => {
                const current = this.userSignal();
                if (current !== null) {
                    this.userSignal.set({ ...current, aiConsentAcceptedAt: new Date().toISOString() });
                }
            }),
            catchError(error => rethrowApiError('Accept AI consent error', error)),
        );
    }

    public revokeAiConsent(): Observable<void> {
        return this.delete<void>('ai-consent').pipe(
            tap(() => {
                const current = this.userSignal();
                if (current !== null) {
                    this.userSignal.set({ ...current, aiConsentAcceptedAt: null });
                }
            }),
            catchError(error => rethrowApiError('Revoke AI consent error', error)),
        );
    }

    public deleteCurrentUser(): Observable<boolean> {
        return this.delete<void>('').pipe(
            tap(() => {
                this.userSignal.set(null);
            }),
            map(() => true),
            catchError(error => fallbackApiError('Delete user error', error, false)),
        );
    }

    public getDesiredWeight(): Observable<number | null> {
        return this.get<DesiredWeightResponse>('desired-weight').pipe(
            map(response => response.desiredWeight ?? null),
            catchError(error => fallbackApiError('Get desired weight error', error, null)),
        );
    }

    public updateDesiredWeight(value: number | null): Observable<number | null> {
        return this.put<DesiredWeightResponse>('desired-weight', {
            desiredWeight: value,
        }).pipe(
            map(response => response.desiredWeight ?? null),
            catchError(error => rethrowApiError('Update desired weight error', error)),
        );
    }

    public getDesiredWaist(): Observable<number | null> {
        return this.get<DesiredWaistResponse>('desired-waist').pipe(
            map(response => response.desiredWaist ?? null),
            catchError(error => fallbackApiError('Get desired waist error', error, null)),
        );
    }

    public updateDesiredWaist(value: number | null): Observable<number | null> {
        return this.put<DesiredWaistResponse>('desired-waist', {
            desiredWaist: value,
        }).pipe(
            map(response => response.desiredWaist ?? null),
            catchError(error => rethrowApiError('Update desired waist error', error)),
        );
    }
}
