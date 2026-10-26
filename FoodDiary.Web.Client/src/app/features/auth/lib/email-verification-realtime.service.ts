import { Service, signal } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';

import { environment } from '../../../../environments/environment';
import type { AuthService } from '../../../services/auth.service';
import { buildRealtimeHubUrl } from '../../../shared/lib/realtime-hub-url.utils';

export function toEmailVerificationHubUrl(authBaseUrl: string): string {
    return buildRealtimeHubUrl(authBaseUrl, '/hubs/email-verification');
}

@Service()
export class EmailVerificationRealtimeService {
    private connection: HubConnection | null = null;
    private generation = 0;
    private connectingGeneration: number | null = null;
    private readonly connectedSignal = signal(false);
    public readonly connected = this.connectedSignal.asReadonly();

    public async connectAsync(authService: AuthService, onVerified: () => void): Promise<void> {
        if (this.connection !== null || this.connectingGeneration !== null) {
            return;
        }

        if (!authService.isAuthenticated()) {
            return;
        }

        const generation = this.generation;
        this.connectingGeneration = generation;
        try {
            const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
            if (generation !== this.generation) {
                return;
            }

            const connection = new HubConnectionBuilder()
                .withUrl(authService.sessionHubUrl(toEmailVerificationHubUrl(environment.apiUrls.auth)), {
                    accessTokenFactory: () => authService.getToken() ?? '',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Warning)
                .build();
            this.connection = connection;
            connection.on('EmailVerified', () => {
                if (this.isCurrentConnection(connection, generation)) {
                    onVerified();
                }
            });

            try {
                await connection.start();
                if (this.isCurrentConnection(connection, generation)) {
                    this.connectedSignal.set(true);
                }
            } catch {
                if (this.isCurrentConnection(connection, generation)) {
                    this.connectedSignal.set(false);
                }
            }
        } finally {
            if (this.connectingGeneration === generation) {
                this.connectingGeneration = null;
            }
        }
    }

    public async disconnectAsync(): Promise<void> {
        const generation = ++this.generation;
        this.connectingGeneration = null;
        const connection = this.connection;
        if (connection === null) {
            return;
        }

        try {
            await connection.stop();
        } finally {
            if (this.isCurrentConnection(connection, generation)) {
                this.connection = null;
                this.connectedSignal.set(false);
            }
        }
    }

    private isCurrentConnection(connection: HubConnection, generation: number): boolean {
        return this.connection === connection && this.generation === generation;
    }
}
