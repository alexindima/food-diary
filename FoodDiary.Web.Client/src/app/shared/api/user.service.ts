import { HttpContext } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, map, type Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { ApiService } from '../../services/api.service';
import { SessionEventsService } from '../auth/session-events.service';
import { fallbackApiError, rethrowApiError } from '../lib/api-error.utils';
import type { DietologistRelationship } from '../models/dietologist.data';
import type {
    ChangePasswordRequest,
    DashboardLayoutSettings,
    DesiredWaistResponse,
    DesiredWeightResponse,
    SetPasswordRequest,
    UpdateUserAppearanceDto,
    UpdateUserDto,
    User,
    WaistGoalHistoryItem,
    WeightGoalHistoryItem,
} from '../models/user.data';
import type { NotificationPreferences, WebPushSubscriptionItem } from '../notifications/notification.service';

export type UserProfileOverview = {
    user: User;
    notificationPreferences: NotificationPreferences;
    webPushSubscriptions: WebPushSubscriptionItem[];
    dietologistRelationship: DietologistRelationship | null;
};

@Service()
export class UserService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.users;
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly silentLoadingContext = new HttpContext().set(SKIP_GLOBAL_LOADING, true);
    private readonly userSignal = signal<User | null>(null);
    public readonly user = this.userSignal.asReadonly();

    public constructor() {
        super();
        this.sessionEvents.authenticated$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.clearUser();
        });
        this.sessionEvents.sessionEnded$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.clearUser();
        });
    }

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
            catchError((error: unknown) => {
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
            catchError((error: unknown) => {
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
            catchError((error: unknown) => {
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
            catchError((error: unknown) => fallbackApiError('Update user error', error, null)),
        );
    }

    public updateAppearance(data: UpdateUserAppearanceDto): Observable<User | null> {
        return this.patch<User>('preferences/appearance', data).pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError((error: unknown) => fallbackApiError('Update user appearance error', error, null)),
        );
    }

    public updateDashboardLayout(layout: DashboardLayoutSettings): Observable<User | null> {
        return this.patch<User>('info', { dashboardLayout: layout }).pipe(
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError((error: unknown) => fallbackApiError('Update dashboard layout error', error, null)),
        );
    }

    public changePassword(request: ChangePasswordRequest): Observable<boolean> {
        return this.patch<void>('password', request).pipe(
            map(() => true),
            catchError((error: unknown) => fallbackApiError('Change password error', error, false)),
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
            catchError((error: unknown) => fallbackApiError('Set password error', error, false)),
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
            catchError((error: unknown) => rethrowApiError('Accept AI consent error', error)),
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
            catchError((error: unknown) => rethrowApiError('Revoke AI consent error', error)),
        );
    }

    public deleteCurrentUser(): Observable<boolean> {
        return this.delete<void>('').pipe(
            tap(() => {
                this.userSignal.set(null);
            }),
            map(() => true),
            catchError((error: unknown) => fallbackApiError('Delete user error', error, false)),
        );
    }

    public getDesiredWeight(): Observable<number | null> {
        return this.get<DesiredWeightResponse>('desired-weight').pipe(
            map(response => response.desiredWeight ?? null),
            catchError((error: unknown) => fallbackApiError('Get desired weight error', error, null)),
        );
    }

    public getWeightGoal(): Observable<DesiredWeightResponse> {
        return this.get<DesiredWeightResponse>('desired-weight').pipe(
            catchError((error: unknown) =>
                fallbackApiError('Get weight goal error', error, {
                    desiredWeight: null,
                    startWeight: null,
                    startedAtUtc: null,
                }),
            ),
        );
    }

    public getWeightGoalHistory(): Observable<WeightGoalHistoryItem[]> {
        return this.get<WeightGoalHistoryItem[]>('weight-goals').pipe(
            catchError((error: unknown) => fallbackApiError('Get weight goal history error', error, [])),
        );
    }

    public updateDesiredWeight(value: number | null): Observable<number | null> {
        return this.put<DesiredWeightResponse>('desired-weight', {
            desiredWeight: value,
        }).pipe(
            map(response => response.desiredWeight ?? null),
            catchError((error: unknown) => rethrowApiError('Update desired weight error', error)),
        );
    }

    public updateWeightGoal(value: number | null): Observable<DesiredWeightResponse> {
        return this.put<DesiredWeightResponse>('desired-weight', { desiredWeight: value }).pipe(
            catchError((error: unknown) => rethrowApiError('Update weight goal error', error)),
        );
    }

    public getDesiredWaist(): Observable<number | null> {
        return this.get<DesiredWaistResponse>('desired-waist').pipe(
            map(response => response.desiredWaist ?? null),
            catchError((error: unknown) => fallbackApiError('Get desired waist error', error, null)),
        );
    }

    public getWaistGoal(): Observable<DesiredWaistResponse> {
        return this.get<DesiredWaistResponse>('desired-waist').pipe(
            catchError((error: unknown) =>
                fallbackApiError('Get waist goal error', error, {
                    desiredWaist: null,
                    startWaist: null,
                    startedAtUtc: null,
                }),
            ),
        );
    }

    public getWaistGoalHistory(): Observable<WaistGoalHistoryItem[]> {
        return this.get<WaistGoalHistoryItem[]>('waist-goals').pipe(
            catchError((error: unknown) => fallbackApiError('Get waist goal history error', error, [])),
        );
    }

    public updateDesiredWaist(value: number | null): Observable<number | null> {
        return this.put<DesiredWaistResponse>('desired-waist', {
            desiredWaist: value,
        }).pipe(
            map(response => response.desiredWaist ?? null),
            catchError((error: unknown) => rethrowApiError('Update desired waist error', error)),
        );
    }

    public updateWaistGoal(value: number | null): Observable<DesiredWaistResponse> {
        return this.put<DesiredWaistResponse>('desired-waist', { desiredWaist: value }).pipe(
            catchError((error: unknown) => rethrowApiError('Update waist goal error', error)),
        );
    }
}
