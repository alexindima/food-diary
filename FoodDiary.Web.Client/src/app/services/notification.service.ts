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

@Injectable({ providedIn: 'root' })
export class NotificationService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly baseUrl = `${environment.apiUrls.auth.replace('/auth', '/notifications')}`;

    public readonly unreadCount = signal(0);

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

    public updateCount(count: number): void {
        this.unreadCount.set(count);
    }
}
