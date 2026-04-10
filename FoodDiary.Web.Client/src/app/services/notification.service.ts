import { Injectable, inject, signal, DestroyRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, tap } from 'rxjs';

export interface NotificationItem {
    id: string;
    type: string;
    title: string;
    body: string | null;
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

@Injectable({ providedIn: 'root' })
export class NotificationService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly baseUrl = `${environment.apiUrls.auth.replace('/auth', '/notifications')}`;

    public readonly unreadCount = signal(0);
    public readonly refreshVersion = signal(0);

    public fetchUnreadCount(): void {
        if (!this.authService.isAuthenticated()) {
            this.unreadCount.set(0);
            return;
        }

        this.http
            .get<{ count: number }>(`${this.baseUrl}/unread-count`)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => this.unreadCount.set(response.count),
                error: () => this.unreadCount.set(0),
            });
    }

    public getNotifications(): Observable<NotificationItem[]> {
        return this.http.get<NotificationItem[]>(this.baseUrl);
    }

    public markAsRead(notificationId: string): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${notificationId}/read`, {}).pipe(
            tap(() => {
                this.unreadCount.update(count => Math.max(0, count - 1));
            }),
        );
    }

    public markAllRead(): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/read-all`, {}).pipe(
            tap(() => {
                this.unreadCount.set(0);
            }),
        );
    }

    public scheduleTestNotification(request: ScheduleTestNotificationRequest): Observable<ScheduledNotificationResponse> {
        return this.http.post<ScheduledNotificationResponse>(`${this.baseUrl}/test/schedule`, request);
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
    }

    public notifyNotificationsChanged(): void {
        this.refreshVersion.update(value => value + 1);
    }
}
