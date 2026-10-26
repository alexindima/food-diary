import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { computed, effect, inject, PLATFORM_ID, Service, signal, untracked } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { FrontendLoggerService } from '../../services/frontend-logger.service';
import { JwtDecoderService } from '../../services/jwt-decoder.service';
import { buildRealtimeHubUrl } from '../lib/realtime-hub-url.utils';
import { NotificationService } from './notification.service';

const RECONCILIATION_INTERVAL_MS = 30_000;

function toNotificationHubUrl(authBaseUrl: string): string {
    return buildRealtimeHubUrl(authBaseUrl, '/hubs/notifications');
}

@Service()
export class NotificationRealtimeService {
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly authService = inject(AuthService);
    private readonly notificationService = inject(NotificationService);
    private readonly logger = inject(FrontendLoggerService);
    private readonly jwtDecoder = inject(JwtDecoderService);
    private connection: HubConnection | null = null;
    private connectionUserId: string | null = null;
    private disconnecting = false;
    private disconnectingTask: Promise<void> | null = null;
    private readonly connecting = signal(false);
    private readonly connectedSignal = signal(false);
    private readonly canConnect = computed(
        () => this.isBrowser && this.authService.isAuthenticated() && !this.authService.isImpersonating(),
    );
    private readonly sessionOwner = computed(() => (this.canConnect() ? this.authService.getUserId() : null));
    private readonly tokenAvailable = computed(() => this.hasSessionToken());

    public readonly connected = this.connectedSignal.asReadonly();

    public constructor() {
        effect(() => {
            if (this.sessionOwner() !== null) {
                untracked(() => {
                    void this.synchronizeConnectionAsync();
                });
                return;
            }

            untracked(() => {
                void this.disconnectAsync();
            });
        });

        effect(onCleanup => {
            if (this.sessionOwner() !== null && this.tokenAvailable()) {
                onCleanup(this.startReconciliation());
            }
        });
    }

    private startReconciliation(): () => void {
        const view = this.document.defaultView;
        if (view === null) {
            return () => {};
        }

        let visible = this.document.visibilityState !== 'hidden';
        let interval: number | null = null;
        const reconcile = (): void => {
            this.reconcileNotifications(this.notificationService.notificationsRequested());
        };
        const stop = (): void => {
            if (interval !== null) {
                view.clearInterval(interval);
                interval = null;
            }
        };
        const start = (): void => {
            interval = view.setInterval(reconcile, RECONCILIATION_INTERVAL_MS);
        };
        const resume = (): void => {
            const nextVisible = this.document.visibilityState !== 'hidden';
            if (visible === nextVisible) {
                return;
            }
            visible = nextVisible;
            stop();
            if (visible) {
                reconcile();
                start();
            }
        };
        if (visible) {
            start();
        }
        this.document.addEventListener('visibilitychange', resume);
        return () => {
            stop();
            this.document.removeEventListener('visibilitychange', resume);
        };
    }

    private reconcileNotifications(refreshFeed: boolean): void {
        if (!this.canConnect() || !this.hasSessionToken() || this.document.visibilityState === 'hidden') {
            return;
        }

        // SignalR is a hint: jobs and other API replicas can commit without reaching this connection.
        this.notificationService.fetchUnreadCount({ force: true });
        if (refreshFeed && !this.notificationService.notificationsLoading()) {
            this.notificationService.refreshNotifications();
        }
    }

    private hasSessionToken(): boolean {
        const key = this.authService.getSessionKey();
        return this.authService.isAuthenticated() && key !== null && key.trim().length > 0;
    }

    private async synchronizeConnectionAsync(): Promise<void> {
        if (this.disconnectingTask !== null) {
            await this.disconnectingTask;
        }
        if (this.connection !== null && this.connectionUserId !== this.authService.getUserId()) {
            await this.disconnectAsync();
        }
        await this.connectAsync();
    }

    private async connectAsync(): Promise<void> {
        if (!this.canStartConnection()) {
            return;
        }

        const token = this.authService.getSessionKey();
        if (token === null || token.trim().length === 0) {
            return;
        }

        this.connecting.set(true);

        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
        const currentToken = this.authService.getSessionKey();
        if (!this.canConnect() || currentToken?.trim() !== token.trim()) {
            this.connecting.set(false);
            return;
        }

        this.connection = new HubConnectionBuilder()
            .withUrl(this.authService.sessionHubUrl(toNotificationHubUrl(environment.apiUrls.auth)), {
                accessTokenFactory: this.getConnectionTokenAsync.bind(this),
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();
        const connection = this.connection;
        const userId = this.authService.getUserId();
        this.connectionUserId = userId;

        this.connection.on('UnreadCountUpdated', (count: number) => {
            if (this.isCurrentConnection(connection, userId)) {
                this.notificationService.updateCount(count);
            }
        });

        this.connection.on('NotificationsChanged', () => {
            if (this.isCurrentConnection(connection, userId)) {
                this.notificationService.notifyNotificationsChanged();
            }
        });

        this.connection.onreconnected(() => {
            if (!this.isCurrentConnection(connection, userId)) {
                return;
            }
            this.notificationService.fetchUnreadCount({ force: true });
            this.notificationService.refreshNotifications();
            this.connectedSignal.set(true);
        });

        this.connection.onclose(error => {
            if (this.connection !== connection) {
                return;
            }
            this.connectedSignal.set(false);
            if (!this.disconnecting && this.canConnect()) {
                this.logger.warn('Notification SignalR connection closed', error, { devOnly: true });
            }
        });

        try {
            await connection.start();
            if (!this.isCurrentConnection(connection, userId)) {
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

    private isCurrentConnection(connection: HubConnection, userId: string | null): boolean {
        return this.connection === connection && this.canConnect() && this.authService.getUserId() === userId;
    }

    private handleConnectionFailure(error: unknown, connection: HubConnection): void {
        if (this.canConnect()) {
            this.logger.error('Notification SignalR connection failed', error, { devOnly: true });
        }
        if (this.connection === connection) {
            this.connection = null;
            this.connectionUserId = null;
        }
    }

    private async disconnectAsync(): Promise<void> {
        if (this.disconnectingTask !== null) {
            return this.disconnectingTask;
        }
        const connection = this.connection;
        if (connection === null) {
            this.connectedSignal.set(false);
            this.connecting.set(false);
            return;
        }

        this.disconnecting = true;
        const task = this.stopConnectionAsync(connection);
        this.disconnectingTask = task;
        try {
            await task;
        } finally {
            if (this.disconnectingTask === task) {
                this.disconnectingTask = null;
            }
        }
    }

    private async stopConnectionAsync(connection: HubConnection): Promise<void> {
        try {
            await connection.stop();
        } finally {
            this.disconnecting = false;
            if (this.connection === connection) {
                this.connection = null;
                this.connectionUserId = null;
                this.connectedSignal.set(false);
                this.connecting.set(false);
            }
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
            const restored = await firstValueFrom(this.authService.refreshSession());
            return restored ? (this.authService.getToken() ?? '') : '';
        }
        return token;
    }
}
