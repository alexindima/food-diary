import { inject, Service } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { SessionEventsService } from './session-events.service';
import type { TelegramOAuthCode, TelegramOAuthState } from './telegram-oidc-values';

const KEY = 'fooddiary.telegram.unlink';
const ATTEMPT_TTL_MS = 300_000;
type UnlinkAttempt = { userId: string; state: string | null; expiresAt: number; errorKey: string | null };

/** Handles persisted unlink callbacks from earlier clients for the initiating account. */
@Service()
export class TelegramUnlinkFlowService {
    private readonly auth = inject(AuthService);
    private readonly storage = inject(BrowserStorageService);
    private sessionRevision = 0;

    public constructor() {
        const sessions = inject(SessionEventsService);
        sessions.authenticated$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.sessionRevision++;
        });
        sessions.sessionEnded$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.sessionRevision++;
            this.clear();
        });
    }

    public async handleCallbackAsync(
        code: TelegramOAuthCode | null,
        state: TelegramOAuthState | null,
        denied: boolean,
    ): Promise<'unhandled' | 'completed' | 'failed'> {
        const attempt = this.storage.getJson('session', KEY);
        if (!isAttempt(attempt) || attempt.state === null) {
            return 'unhandled';
        }
        if (state !== null && state !== attempt.state) {
            return 'unhandled';
        }
        this.clear();
        if (attempt.userId !== this.auth.getUserId()) {
            return 'failed';
        }
        if (code === null || !validCallback(attempt, state, denied)) {
            this.fail(attempt, denied ? 'USER_MANAGE.TELEGRAM_UNLINK_CANCELLED' : 'USER_MANAGE.TELEGRAM_UNLINK_ERROR');
            return 'failed';
        }
        return this.completeAsync(attempt, code, state);
    }

    private async completeAsync(
        attempt: UnlinkAttempt,
        code: TelegramOAuthCode,
        state: TelegramOAuthState,
    ): Promise<'completed' | 'failed'> {
        const revision = this.sessionRevision;
        try {
            await firstValueFrom(this.auth.completeTelegramUnlink(code, state));
            return this.isCurrentSession(attempt.userId, revision) ? 'completed' : 'failed';
        } catch {
            if (this.isCurrentSession(attempt.userId, revision)) {
                this.fail(attempt, 'USER_MANAGE.TELEGRAM_UNLINK_ERROR');
            }
            return 'failed';
        }
    }

    public consumeError(): string | null {
        const attempt = this.storage.getJson('session', KEY);
        if (!isAttempt(attempt) || attempt.userId !== this.auth.getUserId() || attempt.expiresAt <= Date.now()) {
            this.clear();
            return null;
        }
        if (attempt.errorKey === null) {
            return null;
        }
        this.clear();
        return attempt.errorKey;
    }

    private isCurrentSession(userId: string, revision: number): boolean {
        return this.sessionRevision === revision && this.auth.getUserId() === userId;
    }

    private fail(attempt: UnlinkAttempt, errorKey: string): void {
        this.storage.setJson('session', KEY, { ...attempt, state: null, expiresAt: Date.now() + ATTEMPT_TTL_MS, errorKey });
    }

    private clear(): void {
        this.storage.removeItem('session', KEY);
    }
}

function isAttempt(value: unknown): value is UnlinkAttempt {
    if (!isRecord(value)) {
        return false;
    }
    return (
        typeof value['userId'] === 'string' &&
        typeof value['expiresAt'] === 'number' &&
        Number.isFinite(value['expiresAt']) &&
        (value['state'] === null || isState(value['state'])) &&
        isErrorKey(value['errorKey'])
    );
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null;
}

function isState(value: unknown): value is string {
    return typeof value === 'string' && /^[A-Za-z0-9_-]{43}$/.test(value);
}

function isErrorKey(value: unknown): value is string | null {
    return value === null || value === 'USER_MANAGE.TELEGRAM_UNLINK_ERROR' || value === 'USER_MANAGE.TELEGRAM_UNLINK_CANCELLED';
}

function validCallback(attempt: UnlinkAttempt, state: TelegramOAuthState | null, denied: boolean): state is TelegramOAuthState {
    return state !== null && !denied && attempt.expiresAt > Date.now() && state === attempt.state;
}
