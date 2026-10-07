import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, map, type Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { SessionEventsService } from '../auth/session-events.service';
import { fallbackApiError, rethrowApiError } from '../lib/api-error.utils';
import type { DietologistRelationship } from '../models/dietologist.data';
import type { NotificationPreferences, WebPushSubscriptionItem } from '../models/notification.data';
import type { GoalHistoryPage } from '../models/user.data';
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
import { UsersSdk } from './sdk/generated/api/users.service';
import { createSdkConnection, sdkRequestOptions } from './sdk/sdk-connection';
import {
    profileOverviewFromSdk,
    userFromSdk,
    userUpdateToSdk,
    waistGoalFromSdk,
    waistGoalPageFromSdk,
    weightGoalFromSdk,
    weightGoalPageFromSdk,
} from './sdk/user-sdk.mapper';

export type UserProfileOverview = {
    user: User;
    notificationPreferences: NotificationPreferences;
    webPushSubscriptions: WebPushSubscriptionItem[];
    dietologistRelationship: DietologistRelationship | null;
};

@Service()
export class UserService {
    protected readonly baseUrl = environment.apiUrls.users;
    private readonly sdk = createSdkConnection(UsersSdk, this.baseUrl, inject(HttpClient));
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly silentLoadingContext = new HttpContext().set(SKIP_GLOBAL_LOADING, true);
    private readonly userSignal = signal<User | null>(null);
    public readonly user = this.userSignal.asReadonly();

    public constructor() {
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
        return this.sdk.client.getUsersOverview({ version: this.sdk.version }).pipe(
            map(profileOverviewFromSdk),
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
        return this.sdk.client.getUsersInfo({ version: this.sdk.version }).pipe(
            map(userFromSdk),
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
        return this.sdk.client
            .getUsersInfo({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, this.silentLoadingContext))
            .pipe(
                map(userFromSdk),
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
        return this.sdk.client
            .patchUsersInfo({
                version: this.sdk.version,
                updateUserHttpRequest: userUpdateToSdk(data),
            })
            .pipe(
                map(userFromSdk),
                tap(user => {
                    this.userSignal.set(user);
                }),
                catchError((error: unknown) => fallbackApiError('Update user error', error, null)),
            );
    }

    public updateAppearance(data: UpdateUserAppearanceDto): Observable<User | null> {
        return this.sdk.client.patchUsersPreferencesAppearance({ version: this.sdk.version, updateUserAppearanceHttpRequest: data }).pipe(
            map(userFromSdk),
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError((error: unknown) => fallbackApiError('Update user appearance error', error, null)),
        );
    }

    public updateDashboardLayout(layout: DashboardLayoutSettings): Observable<User | null> {
        return this.sdk.client.patchUsersInfo({ version: this.sdk.version, updateUserHttpRequest: { dashboardLayout: layout } }).pipe(
            map(userFromSdk),
            tap(user => {
                this.userSignal.set(user);
            }),
            catchError((error: unknown) => fallbackApiError('Update dashboard layout error', error, null)),
        );
    }

    public changePassword(request: ChangePasswordRequest): Observable<boolean> {
        return this.sdk.client.patchUsersPassword({ version: this.sdk.version, changePasswordHttpRequest: request }).pipe(
            map(() => true),
            catchError((error: unknown) => fallbackApiError('Change password error', error, false)),
        );
    }

    public setPassword(request: SetPasswordRequest): Observable<boolean> {
        return this.sdk.client.patchUsersPasswordSet({ version: this.sdk.version, setPasswordHttpRequest: request }).pipe(
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
        return this.sdk.client.postUsersAiConsent({ version: this.sdk.version }).pipe(
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
        return this.sdk.client.deleteUsersAiConsent({ version: this.sdk.version }).pipe(
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
        return this.sdk.client.deleteUsers({ version: this.sdk.version }).pipe(
            tap(() => {
                this.userSignal.set(null);
            }),
            map(() => true),
            catchError((error: unknown) => fallbackApiError('Delete user error', error, false)),
        );
    }

    public getDesiredWeight(): Observable<number | null> {
        return this.sdk.client.getUsersDesiredWeight({ version: this.sdk.version }).pipe(
            map(response => response.desiredWeightKg ?? null),
            catchError((error: unknown) => fallbackApiError('Get desired weight error', error, null)),
        );
    }

    public getWeightGoal(): Observable<DesiredWeightResponse> {
        return this.sdk.client.getUsersDesiredWeight({ version: this.sdk.version }).pipe(
            map(weightGoalFromSdk),
            catchError((error: unknown) =>
                fallbackApiError('Get weight goal error', error, {
                    desiredWeightKg: null,
                    startWeightKg: null,
                    startedAtUtc: null,
                }),
            ),
        );
    }

    public getWeightGoalHistoryPage(cursor?: string): Observable<GoalHistoryPage<WeightGoalHistoryItem>> {
        return this.sdk.client
            .getUsersWeightGoalsPage(
                { version: this.sdk.version, cursor },
                'body',
                false,
                sdkRequestOptions(undefined, this.silentLoadingContext),
            )
            .pipe(map(weightGoalPageFromSdk));
    }

    public updateDesiredWeight(value: number | null): Observable<number | null> {
        return this.sdk.client
            .putUsersDesiredWeight({ version: this.sdk.version, updateDesiredWeightHttpRequest: { desiredWeightKg: value } })
            .pipe(
                map(response => response.desiredWeightKg ?? null),
                catchError((error: unknown) => rethrowApiError('Update desired weight error', error)),
            );
    }

    public updateWeightGoal(value: number | null): Observable<DesiredWeightResponse> {
        return this.sdk.client
            .putUsersDesiredWeight({ version: this.sdk.version, updateDesiredWeightHttpRequest: { desiredWeightKg: value } })
            .pipe(
                map(weightGoalFromSdk),
                catchError((error: unknown) => rethrowApiError('Update weight goal error', error)),
            );
    }

    public getDesiredWaist(): Observable<number | null> {
        return this.sdk.client.getUsersDesiredWaist({ version: this.sdk.version }).pipe(
            map(response => response.desiredWaistCm ?? null),
            catchError((error: unknown) => fallbackApiError('Get desired waist error', error, null)),
        );
    }

    public getWaistGoal(): Observable<DesiredWaistResponse> {
        return this.sdk.client.getUsersDesiredWaist({ version: this.sdk.version }).pipe(
            map(waistGoalFromSdk),
            catchError((error: unknown) =>
                fallbackApiError('Get waist goal error', error, {
                    desiredWaistCm: null,
                    startWaistCm: null,
                    startedAtUtc: null,
                }),
            ),
        );
    }

    public getWaistGoalHistoryPage(cursor?: string): Observable<GoalHistoryPage<WaistGoalHistoryItem>> {
        return this.sdk.client
            .getUsersWaistGoalsPage(
                { version: this.sdk.version, cursor },
                'body',
                false,
                sdkRequestOptions(undefined, this.silentLoadingContext),
            )
            .pipe(map(waistGoalPageFromSdk));
    }

    public updateDesiredWaist(value: number | null): Observable<number | null> {
        return this.sdk.client
            .putUsersDesiredWaist({ version: this.sdk.version, updateDesiredWaistHttpRequest: { desiredWaistCm: value } })
            .pipe(
                map(response => response.desiredWaistCm ?? null),
                catchError((error: unknown) => rethrowApiError('Update desired waist error', error)),
            );
    }

    public updateWaistGoal(value: number | null): Observable<DesiredWaistResponse> {
        return this.sdk.client
            .putUsersDesiredWaist({ version: this.sdk.version, updateDesiredWaistHttpRequest: { desiredWaistCm: value } })
            .pipe(
                map(waistGoalFromSdk),
                catchError((error: unknown) => rethrowApiError('Update waist goal error', error)),
            );
    }
}
