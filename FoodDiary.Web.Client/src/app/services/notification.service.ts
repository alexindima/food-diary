import { Injectable, inject, signal, DestroyRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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

    public markAllRead(): void {
        this.http
            .put<void>(`${this.baseUrl}/read-all`, {})
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => this.unreadCount.set(0),
            });
    }

    public updateCount(count: number): void {
        this.unreadCount.set(count);
    }
}
