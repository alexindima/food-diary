import { inject, Service } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { SessionEventsService } from './session-events.service';

const KEY = 'fooddiary.telegram.backup-email';
const STATE_LENGTH = 43;
const ATTEMPT_TTL_MS = 300_000;
const EMAIL_TTL_MS = 900_000;
type PendingEmail = {
    userId: string;
    email: string;
    state: string | null;
    expiresAt: number;
    sentAt: number | null;
    failed: boolean;
};

@Service()
export class TelegramBackupEmailFlowService {
    private readonly auth = inject(AuthService);
    private readonly storage = inject(BrowserStorageService);
    private readonly browser = inject(BrowserWindowService);

    public constructor() {
        inject(SessionEventsService)
            .sessionEnded$.pipe(takeUntilDestroyed())
            .subscribe(() => {
                this.clear();
            });
    }

    public read(): PendingEmail | null {
        const value = this.storage.getJson('session', KEY);
        if (!isPendingEmail(value) || value.userId !== this.auth.getUserId() || value.expiresAt <= Date.now()) {
            this.clear();
            return null;
        }
        return value;
    }
    public clear(): void {
        this.storage.removeItem('session', KEY);
    }

    public async startAsync(email: string): Promise<void> {
        const userId = this.auth.getUserId();
        if (userId === null) {
            throw new Error('Authentication required');
        }
        const response = await firstValueFrom(this.auth.startTelegramBackupEmail(email));
        const url = new URL(response.authorizationUrl);
        const state = url.searchParams.get('state');
        if (url.origin !== 'https://oauth.telegram.org' || url.pathname !== '/auth' || state?.length !== STATE_LENGTH) {
            throw new Error('Invalid authorization URL');
        }
        this.storage.setJson('session', KEY, { userId, email, state, expiresAt: Date.now() + ATTEMPT_TTL_MS, sentAt: null, failed: false });
        if (this.read() === null) {
            throw new Error('Session storage unavailable');
        }
        this.browser.open(url.href, '_self');
    }

    public markSent(email: string): void {
        this.storage.setJson('session', KEY, {
            userId: this.auth.getUserId(),
            email,
            state: null,
            expiresAt: Date.now() + EMAIL_TTL_MS,
            sentAt: Date.now(),
            failed: false,
        });
    }

    public async handleCallbackAsync(code: string | null, state: string | null, denied: boolean): Promise<boolean> {
        const pending = this.read();
        if (typeof pending?.state !== 'string') {
            return false;
        }
        this.clear();
        if (denied || code === null || state !== pending.state) {
            this.storage.setJson('session', KEY, { ...pending, state: null, failed: true });
            return true;
        }
        try {
            await firstValueFrom(this.auth.completeTelegramBackupEmail(code, state));
            this.markSent(pending.email);
        } catch {
            this.storage.setJson('session', KEY, { ...pending, state: null, failed: true });
        }
        return true;
    }
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null;
}
function isNullableString(value: unknown): value is string | null {
    return value === null || typeof value === 'string';
}
function isNullableNumber(value: unknown): value is number | null {
    return value === null || (typeof value === 'number' && Number.isFinite(value));
}
function isPendingEmail(value: unknown): value is PendingEmail {
    return (
        isRecord(value) &&
        typeof value['userId'] === 'string' &&
        typeof value['email'] === 'string' &&
        typeof value['expiresAt'] === 'number' &&
        Number.isFinite(value['expiresAt']) &&
        isNullableString(value['state']) &&
        isNullableNumber(value['sentAt']) &&
        typeof value['failed'] === 'boolean'
    );
}
