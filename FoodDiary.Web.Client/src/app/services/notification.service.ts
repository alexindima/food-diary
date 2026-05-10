import { HttpClient, HttpContext } from '@angular/common/http';
import { DestroyRef, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, type Observable, shareReplay, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../constants/global-loading-context.tokens';
import { AuthService } from './auth.service';

export interface NotificationItem {
    id: string;
    type: string;
    title: string;
    body: string | null;
    targetUrl: string | null;
    referenceId: string | null;
    isRead: boolean;
    createdAtUtc: string;
}

export interface ScheduleTestNotificationRequest {
    delaySeconds: number;
    type: string;
}

export interface ScheduledNotificationResponse {
    type: string;
    delaySeconds: number;
    scheduledAtUtc: string;
}

export interface WebPushConfiguration {
    enabled: boolean;
    publicKey: string | null;
}

export interface WebPushSubscriptionRequest {
    endpoint: string;
    expirationTime: string | null;
    keys: {
        p256dh: string;
        auth: string;
    };
    locale: string | null;
    userAgent: string | null;
}

export interface NotificationPreferences {
    pushNotificationsEnabled: boolean;
    fastingPushNotificationsEnabled: boolean;
    socialPushNotificationsEnabled: boolean;
    fastingCheckInReminderHours: number;
    fastingCheckInFollowUpReminderHours: number;
}

export interface WebPushSubscriptionItem {
    endpoint: string;
    endpointHost: string;
    expirationTimeUtc: string | null;
    locale: string | null;
    userAgent: string | null;
    createdAtUtc: string;
    updatedAtUtc: string | null;
}

export interface UpdateNotificationPreferencesRequest {
    pushNotificationsEnabled?: boolean;
    fastingPushNotificationsEnabled?: boolean;
    socialPushNotificationsEnabled?: boolean;
    fastingCheckInReminderHours?: number;
    fastingCheckInFollowUpReminderHours?: number;
}

interface FetchUnreadCountOptions {
    force?: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly baseUrl = environment.apiUrls.auth.replace('/auth', '/notifications');
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

        if (this.unreadCountRequest$ || (this.unreadCountLoaded() && !options.force)) {
            return;
        }

        this.unreadCountRequest$ = this.http
            .get<{ count: number }>(`${this.baseUrl}/unread-count`, {
                context: this.silentLoadingContext,
            })
            .pipe(
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

    public getNotifications(): Observable<NotificationItem[]> {
        return this.http.get<NotificationItem[]>(this.baseUrl, {
            context: this.silentLoadingContext,
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

    public markAsRead(notificationId: string): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${notificationId}/read`, {}).pipe(
            tap(() => {
                this.unreadCount.update(count => Math.max(0, count - 1));
                this.notifications.update(items => items.map(item => (item.id === notificationId ? { ...item, isRead: true } : item)));
            }),
        );
    }

    public markAllRead(): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/read-all`, {}).pipe(
            tap(() => {
                this.unreadCount.set(0);
                this.notifications.update(items => items.map(item => ({ ...item, isRead: true })));
            }),
        );
    }

    public scheduleTestNotification(request: ScheduleTestNotificationRequest): Observable<ScheduledNotificationResponse> {
        return this.http.post<ScheduledNotificationResponse>(`${this.baseUrl}/test/schedule`, request);
    }

    public getNotificationPreferences(): Observable<NotificationPreferences> {
        return this.http.get<NotificationPreferences>(`${this.baseUrl}/preferences`);
    }

    public updateNotificationPreferences(request: UpdateNotificationPreferencesRequest): Observable<NotificationPreferences> {
        return this.http.put<NotificationPreferences>(`${this.baseUrl}/preferences`, request);
    }

    public getWebPushSubscriptions(): Observable<WebPushSubscriptionItem[]> {
        return this.http.get<WebPushSubscriptionItem[]>(`${this.baseUrl}/push/subscriptions`);
    }

    public getWebPushConfiguration(): Observable<WebPushConfiguration> {
        return this.http.get<WebPushConfiguration>(`${this.baseUrl}/push/config`);
    }

    public upsertWebPushSubscription(request: WebPushSubscriptionRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/push/subscription`, request);
    }

    public removeWebPushSubscription(endpoint: string): Observable<void> {
        return this.http.request<void>('DELETE', `${this.baseUrl}/push/subscription`, {
            body: { endpoint },
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
        this.http
            .get<NotificationItem[]>(this.baseUrl, {
                context: this.silentLoadingContext,
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: notifications => {
                    this.notifications.set(notifications);
                    this.notificationsLoaded.set(true);
                    this.notificationsLoading.set(false);
                },
                error: () => {
                    this.notifications.set([]);
                    this.notificationsLoaded.set(true);
                    this.notificationsLoading.set(false);
                },
            });
    }
}
