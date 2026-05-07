import { effect, inject, Injectable, signal, untracked } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';

import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { FrontendLoggerService } from './frontend-logger.service';
import { NotificationService } from './notification.service';

function toNotificationHubUrl(authBaseUrl: string): string {
    return `${authBaseUrl.replace(/\/api(?:\/v\d+(?:\.\d+)?)?\/auth$/, '')}/hubs/notifications`;
}

@Injectable({
    providedIn: 'root',
})
export class NotificationRealtimeService {
    private readonly authService = inject(AuthService);
    private readonly notificationService = inject(NotificationService);
    private readonly logger = inject(FrontendLoggerService);
    private connection: HubConnection | null = null;
    private readonly connecting = signal(false);
    private readonly connectedSignal = signal(false);

    public readonly connected = this.connectedSignal.asReadonly();

    public constructor() {
        effect(() => {
            if (this.authService.isAuthenticated()) {
                untracked(() => {
                    void this.connectAsync();
                });
                return;
            }

            untracked(() => {
                void this.disconnectAsync();
            });
        });
    }

    private async connectAsync(): Promise<void> {
        if (this.connection || this.connecting()) {
            return;
        }

        const token = this.authService.getToken();
        if (!token) {
            return;
        }

        this.connecting.set(true);

        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');

        this.connection = new HubConnectionBuilder()
            .withUrl(toNotificationHubUrl(environment.apiUrls.auth), {
                accessTokenFactory: () => this.authService.getToken() ?? '',
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();

        this.connection.on('UnreadCountUpdated', (count: number) => {
            this.notificationService.updateCount(count);
        });

        this.connection.on('NotificationsChanged', () => {
            this.notificationService.notifyNotificationsChanged();
        });

        this.connection.onreconnected(() => {
            this.notificationService.fetchUnreadCount({ force: true });
            this.notificationService.refreshNotifications();
            this.connectedSignal.set(true);
        });

        this.connection.onclose(() => {
            this.connectedSignal.set(false);
            this.logger.warn('Notification SignalR connection closed', undefined, { devOnly: true });
        });

        try {
            await this.connection.start();
            this.connectedSignal.set(true);
            this.notificationService.fetchUnreadCount();
            this.notificationService.ensureNotificationsLoaded();
        } catch (error) {
            this.connectedSignal.set(false);
            this.logger.error('Notification SignalR connection failed', error, { devOnly: true });
            this.connection = null;
        } finally {
            this.connecting.set(false);
        }
    }

    private async disconnectAsync(): Promise<void> {
        if (!this.connection) {
            this.connectedSignal.set(false);
            return;
        }

        try {
            await this.connection.stop();
        } finally {
            this.connection = null;
            this.connectedSignal.set(false);
            this.connecting.set(false);
        }
    }
}
