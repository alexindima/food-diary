import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

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

        const baseUrl = environment.apiUrls.auth.replace(/\/api\/auth$/, '');
        this.connection = new HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/email-verification`, {
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
