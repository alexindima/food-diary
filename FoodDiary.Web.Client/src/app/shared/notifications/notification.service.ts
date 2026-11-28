import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { computed, DestroyRef, effect, inject, Service, signal, untracked } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, map, merge, type Observable, Subject, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { SKIP_AUTH } from '../../constants/http-context.tokens';
import { AuthService } from '../../services/auth.service';
import { NotificationsSdk } from '../api/sdk/generated/api/notifications.service';
import { notificationFromSdk, notificationPreferencesFromSdk, webPushSubscriptionFromSdk } from '../api/sdk/notification-sdk.mapper';
import { createSdkConnection, sdkRequestOptions } from '../api/sdk/sdk-connection';
import { requireSdkFields, sdkNullableFields } from '../api/sdk/sdk-response';
import { SessionEventsService } from '../auth/session-events.service';
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
import { parsePushNotificationId } from './push-notification-reference';
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
    supersedePending?: boolean;
};

type NotificationState = {
    unreadCount: number | null;
    items: readonly NotificationItem[];
    loading: boolean;
    loaded: boolean;
    requested: boolean;
    version: number;
};

@Service()
export class NotificationService {
    private readonly readStateChanged = new Subject<void>();
    public readonly readStateChanged$ = this.readStateChanged.asObservable();
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');
    private readonly sdk = createSdkConnection(NotificationsSdk, this.baseUrl, this.http);
    private readonly silentLoadingContext = new HttpContext().set(SKIP_GLOBAL_LOADING, true);

    public readonly unreadCount = signal(0);
    public readonly notifications = signal<NotificationItem[]>([]);
    public readonly notificationsLoading = signal(false);
    public readonly notificationsLoaded = signal(false);
    public readonly notificationsRequested = signal(false);
    public readonly notificationsChangedVersion = signal(0);
    private readonly unreadCountLoaded = signal(false);
    private unreadCountRevision = 0;
    private notificationsReloadRequested = false;
    private sessionOwner = this.readSessionOwner();
    private sessionVersion = 0;
    private readonly unreadCountRequest = signal<{ session: number } | undefined>(undefined);
    private readonly unreadCountResource = rxResource({
        params: this.unreadCountRequest,
        stream: ({ params, abortSignal }) =>
            this.sdk.client
                .getNotificationsUnreadCount(
                    { version: this.sdk.version },
                    'body',
                    false,
                    sdkRequestOptions(undefined, this.silentLoadingContext),
                )
                .pipe(
                    map(value => requireSdkFields(value, ['count'])),
                    tap(response => {
                        if (!abortSignal.aborted && this.isCurrentSession(params.session) && this.unreadCountRequest() === params) {
                            this.setServerUnreadCount(response.count);
                        }
                    }),
                ),
    });
    private readonly notificationsRequest = signal<{ session: number } | undefined>(undefined);
    private readonly notificationsResource = rxResource({
        params: this.notificationsRequest,
        stream: ({ params, abortSignal }) =>
            this.sdk.client
                .getNotifications({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, this.silentLoadingContext))
                .pipe(
                    map(values => values.map(notificationFromSdk)),
                    tap(notifications => {
                        if (
                            !abortSignal.aborted &&
                            this.notificationsRequest() === params &&
                            this.isCurrentSession(params.session) &&
                            !this.notificationsReloadRequested
                        ) {
                            this.notifications.set(notifications);
                            this.notificationsLoaded.set(true);
                        }
                    }),
                    finalize(() => {
                        if (!abortSignal.aborted && this.notificationsRequest() === params && this.isCurrentSession(params.session)) {
                            this.notificationsLoading.set(false);
                            // Wait for the current response, then honor a queued refresh before accepting any rows.
                            if (this.notificationsReloadRequested) {
                                this.notificationsReloadRequested = false;
                                this.loadNotifications();
                            }
                        }
                    }),
                ),
    });
    public readonly hasNotificationsLoadError = computed(() => this.notificationsResource.error() !== undefined);

    public constructor() {
        effect(() => {
            const owner = this.readSessionOwner();
            untracked(() => {
                this.synchronizeSession(owner);
            });
        });
        merge(this.sessionEvents.authenticated$, this.sessionEvents.sessionEnded$)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.resetSession();
            });
    }

    public fetchUnreadCount(options: FetchUnreadCountOptions = {}): void {
        const session = this.synchronizeSession();
        if (this.sessionOwner === null) {
            return;
        }

        if (
            (untracked(this.unreadCountResource.isLoading) && options.supersedePending !== true) ||
            (this.unreadCountLoaded() && options.force !== true)
        ) {
            return;
        }

        this.unreadCountResource.set(undefined);
        this.unreadCountRequest.set({ session });
    }

    public readState(): NotificationState {
        this.synchronizeSession();
        return {
            unreadCount: this.unreadCountLoaded() ? this.unreadCount() : null,
            items: [...this.notifications()],
            loading: this.notificationsLoading(),
            loaded: this.notificationsLoaded(),
            requested: this.notificationsRequested(),
            version: this.notificationsChangedVersion(),
        };
    }

    public ensureNotificationsLoaded(): void {
        this.synchronizeSession();
        if (this.notificationsLoaded() || this.notificationsLoading()) {
            return;
        }

        this.loadNotifications();
    }

    public refreshNotifications(options: { supersedePending?: boolean } = {}): void {
        this.synchronizeSession();
        if (this.notificationsLoading()) {
            if (options.supersedePending === true) {
                this.notificationsReloadRequested = true;
            }
            return;
        }
        this.loadNotifications();
    }

    public markAsRead(notificationId: NotificationId): Observable<void> {
        const session = this.synchronizeSession();
        const countRevision = this.unreadCountRevision;
        return this.sdk.client.putNotificationsByNotificationIdRead({ version: this.sdk.version, notificationId }).pipe(
            tap(() => {
                if (!this.isCurrentSession(session)) {
                    return;
                }
                const serverCountUpdated = this.unreadCountRevision !== countRevision;
                const notification = this.notifications().find(item => item.id === notificationId);
                if (!serverCountUpdated && notification?.isRead === false) {
                    this.unreadCount.update(count => Math.max(0, count - 1));
                }

                this.notifications.update(items => items.map(item => (item.id === notificationId ? { ...item, isRead: true } : item)));
                this.acknowledgeReadState({ refreshCount: serverCountUpdated });
            }),
        );
    }

    public markAllRead(): Observable<void> {
        const session = this.synchronizeSession();
        const countRevision = this.unreadCountRevision;
        const originalFeed = this.notifications();
        const originalIds = new Set(originalFeed.map(item => item.id));
        return this.sdk.client.putNotificationsReadAll({ version: this.sdk.version }).pipe(
            tap(() => {
                if (!this.isCurrentSession(session)) {
                    return;
                }
                const feedUpdated = this.notifications() !== originalFeed;
                const stateUpdated = feedUpdated || this.unreadCountRevision !== countRevision;
                if (!stateUpdated) {
                    this.unreadCount.set(0);
                }
                // Notifications that appeared during the request may have arrived after the server marked the old ones read.
                this.notifications.update(items =>
                    items.map(item => (originalIds.has(item.id) && !item.isRead ? { ...item, isRead: true } : item)),
                );
                this.acknowledgeReadState({ refreshCount: stateUpdated || this.notificationsLoading(), refreshFeed: feedUpdated });
            }),
        );
    }

    public scheduleTestNotification(request: ScheduleTestNotificationRequest): Observable<ScheduledNotificationResponse> {
        return this.sdk.client
            .postNotificationsTestSchedule({ version: this.sdk.version, scheduleTestNotificationHttpRequest: request })
            .pipe(map(value => requireSdkFields(value, ['type', 'delaySeconds', 'scheduledAtUtc'])));
    }

    public getReadNotificationIds(notificationIds: readonly NotificationId[]): Observable<NotificationId[]> {
        return this.sdk.client
            .postNotificationsReadStatus(
                { version: this.sdk.version, notificationReadStatusHttpRequest: { notificationIds: [...notificationIds] } },
                'body',
                false,
                sdkRequestOptions(undefined, this.silentLoadingContext),
            )
            .pipe(
                map(response =>
                    requireSdkFields(response, ['readIds']).readIds.map(value => {
                        const id = parsePushNotificationId(value);
                        if (id === null) {
                            throw new Error('API response contains an invalid notification id.');
                        }
                        return id;
                    }),
                ),
            );
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

    public removeWebPushSubscription(endpoint: string, sessionToken?: string): Observable<void> {
        return this.sdk.client.deleteNotificationsPushSubscription(
            { version: this.sdk.version, removeWebPushSubscriptionHttpRequest: { endpoint } },
            'body',
            false,
            this.webPushSessionOptions(sessionToken),
        );
    }

    private webPushSessionOptions(sessionToken?: string): { context: HttpContext } {
        return sessionToken === undefined
            ? sdkRequestOptions()
            : sdkRequestOptions(
                  new HttpHeaders(
                      sessionToken.includes('.') ? { Authorization: `Bearer ${sessionToken}` } : { 'X-FoodDiary-Session': sessionToken },
                  ),
                  new HttpContext().set(SKIP_AUTH, true),
              );
    }

    public updateCount(count: number): void {
        this.synchronizeSession();
        if (this.sessionOwner === null) {
            return;
        }
        this.setServerUnreadCount(count);
    }

    public notifyNotificationsChanged(options: { supersedePending?: boolean } = {}): void {
        this.synchronizeSession();
        if (this.sessionOwner === null) {
            return;
        }
        this.notificationsChangedVersion.update(version => version + 1);
        this.readStateChanged.next();
        this.refreshNotifications(options);
    }

    private setServerUnreadCount(count: number): void {
        this.unreadCountRevision++;
        this.unreadCount.set(count);
        this.unreadCountLoaded.set(true);
        this.readStateChanged.next();
    }

    private acknowledgeReadState({ refreshCount, refreshFeed }: { refreshCount: boolean; refreshFeed?: boolean }): void {
        // Reads started before the successful write may still contain the previous unread state.
        // A count received during the write may already include it, so reconcile from the server.
        if (refreshCount || untracked(this.unreadCountResource.isLoading)) {
            this.fetchUnreadCount({ force: true, supersedePending: true });
        }
        if (refreshFeed === true || this.notificationsLoading()) {
            this.refreshNotifications({ supersedePending: true });
        }
        this.readStateChanged.next();
    }

    private loadNotifications(): void {
        const session = this.synchronizeSession();
        if (this.sessionOwner === null || this.notificationsLoading()) {
            return;
        }

        this.notificationsRequested.set(true);
        this.notificationsLoading.set(true);
        this.notificationsRequest.set({ session });
    }

    private readSessionOwner(): string | null {
        return this.authService.isAuthenticated()
            ? `${this.authService.getUserId() ?? 'pending'}:${this.authService.isImpersonating() ? 'impersonated' : 'normal'}`
            : null;
    }

    private synchronizeSession(owner = this.readSessionOwner()): number {
        if (owner !== this.sessionOwner) {
            this.sessionOwner = owner;
            this.resetSession();
        }
        return this.sessionVersion;
    }

    private isCurrentSession(version: number): boolean {
        return version === this.sessionVersion && this.sessionOwner !== null && this.sessionOwner === this.readSessionOwner();
    }

    private resetSession(): void {
        this.sessionVersion++;
        this.unreadCountRequest.set(undefined);
        this.unreadCountResource.set(undefined);
        this.notificationsRequest.set(undefined);
        this.notificationsResource.set(undefined);
        this.unreadCount.set(0);
        this.unreadCountLoaded.set(false);
        this.unreadCountRevision = 0;
        this.notifications.set([]);
        this.notificationsLoading.set(false);
        this.notificationsLoaded.set(false);
        this.notificationsRequested.set(false);
        this.notificationsReloadRequested = false;
        this.notificationsChangedVersion.set(0);
    }
}
