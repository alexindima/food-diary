import { inject, Service } from '@angular/core';

import { BrowserStorageService } from '../platform/browser-storage.service';

const SESSION_ID_STORAGE_KEY = 'fd_telemetry_session_id';
const SESSION_ID_PREFIX = 'fd-session';
const RANDOM_RADIX = 36;

@Service()
export class ClientTelemetrySessionService {
    private readonly storage = inject(BrowserStorageService);

    public getSessionId(): string {
        const stored = this.storage.getItem('session', SESSION_ID_STORAGE_KEY);
        if (stored !== null && stored.length > 0) {
            return stored;
        }

        const sessionId = this.createSessionId();
        this.storage.setItem('session', SESSION_ID_STORAGE_KEY, sessionId);
        return sessionId;
    }

    private createSessionId(): string {
        const timestamp = Date.now().toString(RANDOM_RADIX);
        const random = Math.random().toString(RANDOM_RADIX).slice(2);
        return `${SESSION_ID_PREFIX}-${timestamp}-${random}`;
    }
}
