import { computed, effect, inject, Service, signal, untracked } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { FrontendLoggerService } from '../../services/frontend-logger.service';
import { JwtDecoderService } from '../../services/jwt-decoder.service';
import { buildRealtimeHubUrl } from '../lib/realtime-hub-url.utils';
import { NotificationService } from './notification.service';

function toNotificationHubUrl(authBaseUrl: string): string {
    return buildRealtimeHubUrl(authBaseUrl, '/hubs/notifications');
}

@Service()
export class NotificationRealtimeService {
    private readonly authService = inject(AuthService);
    private readonly notificationService = inject(NotificationService);
    private readonly logger = inject(FrontendLoggerService);
    private readonly jwtDecoder = inject(JwtDecoderService);
    private connection: HubConnection | null = null;
    private disconnecting = false;
    private readonly connecting = signal(false);
    private readonly connectedSignal = signal(false);
    private readonly canConnect = computed(() => this.authService.isAuthenticated() && !this.authService.isImpersonating());

    public readonly connected = this.connectedSignal.asReadonly();

    public constructor() {
        effect(() => {
            if (this.canConnect()) {
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
        if (!this.canStartConnection()) {
            return;
        }

        const token = this.authService.getToken();
        if (token === null || token.trim().length === 0) {
            return;
        }

        this.connecting.set(true);

        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
        const currentToken = this.authService.getToken();
        if (!this.canConnect() || currentToken?.trim() !== token.trim()) {
            this.connecting.set(false);
            return;
        }

        this.connection = new HubConnectionBuilder()
            .withUrl(toNotificationHubUrl(environment.apiUrls.auth), {
                accessTokenFactory: this.getConnectionTokenAsync.bind(this),
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();
        const connection = this.connection;

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

        this.connection.onclose(error => {
            this.connectedSignal.set(false);
            if (!this.disconnecting && this.canConnect()) {
                this.logger.warn('Notification SignalR connection closed', error, { devOnly: true });
            }
        });

        try {
            await connection.start();
            if (this.connection !== connection || !this.canConnect()) {
                return;
            }
            this.connectedSignal.set(true);
            this.notificationService.fetchUnreadCount();
            this.notificationService.ensureNotificationsLoaded();
        } catch (error) {
            this.connectedSignal.set(false);
            this.handleConnectionFailure(error, connection);
        } finally {
            this.connecting.set(false);
        }
    }

    private canStartConnection(): boolean {
        return this.canConnect() && this.connection === null && !this.connecting();
    }

    private handleConnectionFailure(error: unknown, connection: HubConnection): void {
        if (this.canConnect()) {
            this.logger.error('Notification SignalR connection failed', error, { devOnly: true });
        }
        if (this.connection === connection) {
            this.connection = null;
        }
    }

    private async disconnectAsync(): Promise<void> {
        if (this.connection === null) {
            this.connectedSignal.set(false);
            this.connecting.set(false);
            return;
        }

        try {
            this.disconnecting = true;
            await this.connection.stop();
        } finally {
            this.disconnecting = false;
            this.connection = null;
            this.connectedSignal.set(false);
            this.connecting.set(false);
        }
    }

    private async getConnectionTokenAsync(): Promise<string> {
        if (!this.canConnect()) {
            return '';
        }
        const token = this.authService.getToken();
        if (typeof token !== 'string') {
            return '';
        }
        if (this.jwtDecoder.isExpired(token)) {
            return (await firstValueFrom(this.authService.refreshToken())) ?? '';
        }
        return token;
    }
}
