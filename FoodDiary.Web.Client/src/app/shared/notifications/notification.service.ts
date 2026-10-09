import { HttpClient, HttpContext } from '@angular/common/http';
import { DestroyRef, effect, inject, Service, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, map, type Observable, shareReplay, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { AuthService } from '../../services/auth.service';
import { NotificationsSdk } from '../api/sdk/generated/api/notifications.service';
import { notificationFromSdk, notificationPreferencesFromSdk, webPushSubscriptionFromSdk } from '../api/sdk/notification-sdk.mapper';
import { createSdkConnection, sdkRequestOptions } from '../api/sdk/sdk-connection';
import { requireSdkFields, sdkNullableFields } from '../api/sdk/sdk-response';
import type {
    NotificationItem,
    NotificationPreferences,
    ScheduledNotificationResponse,
    ScheduleTestNotificationRequest,
    UpdateNotificationPreferencesRequest,
    WebPushConfiguration,
    WebPushSubscriptionItem,
    WebPushSubscriptionRequest,
} from '../models/notification.data';
import type { NotificationId } from '../models/semantics/entity-id';
export type {
    NotificationItem,
    NotificationPreferences,
    ScheduledNotificationResponse,
    ScheduleTestNotificationRequest,
    UpdateNotificationPreferencesRequest,
    WebPushConfiguration,
    WebPushSubscriptionItem,
    WebPushSubscriptionRequest,
} from '../models/notification.data';

type FetchUnreadCountOptions = {
    force?: boolean;
};

@Service()
export class NotificationService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');
    private readonly sdk = createSdkConnection(NotificationsSdk, this.baseUrl, this.http);
    private readonly silentLoadingContext = new HttpContext().set(SKIP_GLOBAL_LOADING, true);

    public readonly unreadCount = signal(0);
    public readonly notifications = signal<NotificationItem[]>([]);
    public readonly notificationsLoading = signal(false);
    public readonly notificationsLoaded = signal(false);
    public readonly notificationsChangedVersion = signal(0);
    private readonly unreadCountLoaded = signal(false);
    private unreadCountRequest$: Observable<{ count: number }> | null = null;

    public constructor() {
        effect(() => {
            if (this.authService.isAuthenticated()) {
                return;
            }

            untracked(() => {
                this.unreadCount.set(0);
                this.unreadCountLoaded.set(false);
                this.notifications.set([]);
                this.notificationsLoading.set(false);
                this.notificationsLoaded.set(false);
            });
        });
    }

    public fetchUnreadCount(options: FetchUnreadCountOptions = {}): void {
        if (!this.authService.isAuthenticated()) {
            this.unreadCount.set(0);
            this.unreadCountLoaded.set(false);
            return;
        }

        if (this.unreadCountRequest$ !== null || (this.unreadCountLoaded() && options.force !== true)) {
            return;
        }

        this.unreadCountRequest$ = this.sdk.client
            .getNotificationsUnreadCount(
                { version: this.sdk.version },
                'body',
                false,
                sdkRequestOptions(undefined, this.silentLoadingContext),
            )
            .pipe(
                map(value => requireSdkFields(value, ['count'])),
                finalize(() => {
                    this.unreadCountRequest$ = null;
                }),
                shareReplay({ bufferSize: 1, refCount: false }),
            );

        this.unreadCountRequest$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: response => {
                this.unreadCount.set(response.count);
                this.unreadCountLoaded.set(true);
            },
            error: () => {
                this.unreadCount.set(0);
                this.unreadCountLoaded.set(false);
            },
        });
    }

    public ensureNotificationsLoaded(): void {
        if (this.notificationsLoaded() || this.notificationsLoading()) {
            return;
        }

        this.loadNotifications();
    }

    public refreshNotifications(): void {
        if (!this.authService.isAuthenticated()) {
            this.notifications.set([]);
            this.notificationsLoaded.set(false);
            return;
        }

        this.loadNotifications();
    }

    public markAsRead(notificationId: NotificationId): Observable<void> {
        return this.sdk.client.putNotificationsByNotificationIdRead({ version: this.sdk.version, notificationId }).pipe(
            tap(() => {
                const notification = this.notifications().find(item => item.id === notificationId);
                if (notification?.isRead === false) {
                    this.unreadCount.update(count => Math.max(0, count - 1));
                }

                this.notifications.update(items => items.map(item => (item.id === notificationId ? { ...item, isRead: true } : item)));
            }),
        );
    }

    public markAllRead(): Observable<void> {
        return this.sdk.client.putNotificationsReadAll({ version: this.sdk.version }).pipe(
            tap(() => {
                this.unreadCount.set(0);
                this.notifications.update(items => items.map(item => ({ ...item, isRead: true })));
            }),
        );
    }

    public scheduleTestNotification(request: ScheduleTestNotificationRequest): Observable<ScheduledNotificationResponse> {
        return this.sdk.client
            .postNotificationsTestSchedule({ version: this.sdk.version, scheduleTestNotificationHttpRequest: request })
            .pipe(map(value => requireSdkFields(value, ['type', 'delaySeconds', 'scheduledAtUtc'])));
    }

    public getNotificationPreferences(): Observable<NotificationPreferences> {
        return this.sdk.client.getNotificationsPreferences({ version: this.sdk.version }).pipe(map(notificationPreferencesFromSdk));
    }

    public updateNotificationPreferences(request: UpdateNotificationPreferencesRequest): Observable<NotificationPreferences> {
        return this.sdk.client
            .putNotificationsPreferences({ version: this.sdk.version, updateNotificationPreferencesHttpRequest: request })
            .pipe(map(notificationPreferencesFromSdk));
    }

    public getWebPushSubscriptions(): Observable<WebPushSubscriptionItem[]> {
        return this.sdk.client
            .getNotificationsPushSubscriptions({ version: this.sdk.version })
            .pipe(map(values => values.map(webPushSubscriptionFromSdk)));
    }

    public getWebPushConfiguration(): Observable<WebPushConfiguration> {
        return this.sdk.client
            .getNotificationsPushConfig({ version: this.sdk.version })
            .pipe(map(value => sdkNullableFields(requireSdkFields(value, ['enabled']), ['publicKey'])));
    }

    public upsertWebPushSubscription(request: WebPushSubscriptionRequest): Observable<void> {
        return this.sdk.client.putNotificationsPushSubscription({
            version: this.sdk.version,
            upsertWebPushSubscriptionHttpRequest: request,
        });
    }

    public removeWebPushSubscription(endpoint: string): Observable<void> {
        return this.sdk.client.deleteNotificationsPushSubscription({
            version: this.sdk.version,
            removeWebPushSubscriptionHttpRequest: { endpoint },
        });
    }

    public updateCount(count: number): void {
        this.unreadCount.set(count);
        this.unreadCountLoaded.set(true);
    }

    public notifyNotificationsChanged(): void {
        this.notificationsChangedVersion.update(version => version + 1);
        this.refreshNotifications();
    }

    private loadNotifications(): void {
        if (!this.authService.isAuthenticated()) {
            this.notifications.set([]);
            this.notificationsLoading.set(false);
            this.notificationsLoaded.set(false);
            return;
        }

        this.notificationsLoading.set(true);
        this.sdk.client
            .getNotifications({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, this.silentLoadingContext))
            .pipe(
                map(values => values.map(notificationFromSdk)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: notifications => {
                    this.notifications.set(notifications);
                    this.notificationsLoaded.set(true);
                    this.notificationsLoading.set(false);
                },
                error: () => {
                    this.notifications.set([]);
                    this.notificationsLoaded.set(false);
                    this.notificationsLoading.set(false);
                },
            });
    }
}
