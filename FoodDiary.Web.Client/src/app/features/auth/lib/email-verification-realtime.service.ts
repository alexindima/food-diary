import { Injectable, signal } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';

import { environment } from '../../../../environments/environment';
import { type AuthService } from '../../../services/auth.service';

export function toEmailVerificationHubUrl(authBaseUrl: string): string {
    return `${authBaseUrl.replace(/\/api(?:\/v\d+(?:\.\d+)?)?\/auth$/, '')}/hubs/email-verification`;
}

@Injectable({
    providedIn: 'root',
})
export class EmailVerificationRealtimeService {
    private connection: HubConnection | null = null;
    private readonly connectedSignal = signal(false);
    public readonly connected = this.connectedSignal.asReadonly();

    public async connect(authService: AuthService, onVerified: () => void): Promise<void> {
        if (this.connection) {
            return;
        }

        const token = authService.getToken();
        if (!token) {
            return;
        }

        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');

        this.connection = new HubConnectionBuilder()
            .withUrl(toEmailVerificationHubUrl(environment.apiUrls.auth), {
                accessTokenFactory: () => authService.getToken() ?? '',
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();

        this.connection.on('EmailVerified', () => {
            onVerified();
        });

        try {
            await this.connection.start();
            this.connectedSignal.set(true);
        } catch {
            this.connectedSignal.set(false);
        }
    }

    public async disconnect(): Promise<void> {
        if (!this.connection) {
            return;
        }

        try {
            await this.connection.stop();
        } finally {
            this.connection = null;
            this.connectedSignal.set(false);
        }
    }
}
