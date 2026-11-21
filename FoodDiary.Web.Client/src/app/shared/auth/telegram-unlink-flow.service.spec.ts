import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { SessionEventsService } from './session-events.service';
import { telegramOAuthCode, telegramOAuthState } from './telegram-oidc-values';
import { TelegramUnlinkFlowService } from './telegram-unlink-flow.service';

const STATE_LENGTH = 43;
const ATTEMPT_TTL_MS = 300_000;
const state = 's'.repeat(STATE_LENGTH);
const ended = new Subject<void>();
const authenticated = new Subject<void>();
const auth = { getUserId: vi.fn(), completeTelegramUnlink: vi.fn() };
let stored: unknown;
let flow: TelegramUnlinkFlowService;

describe('Telegram unlink callback ownership', () => {
    it('leaves unrelated Telegram callbacks available to their owning flow', async () => {
        storeLegacyAttempt();
        expect(
            await flow.handleCallbackAsync(telegramOAuthCode('other-code'), telegramOAuthState('x'.repeat(STATE_LENGTH)), false),
            'Unrelated Telegram callbacks must keep their own flow',
        ).toBe('unhandled');
        expect(auth.completeTelegramUnlink).not.toHaveBeenCalled();
        expect(await flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false)).toBe('completed');
    });

    it('does not end a newer session of the same account after a delayed completion', async () => {
        storeLegacyAttempt();
        const response = new Subject<void>();
        auth.completeTelegramUnlink.mockReturnValue(response);
        const completion = flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false);
        authenticated.next();
        response.next();
        expect(await completion).toBe('failed');
    });

    it('does not restore an old attempt when a completion fails after logout', async () => {
        storeLegacyAttempt();
        const response = new Subject<void>();
        auth.completeTelegramUnlink.mockReturnValue(response);
        const completion = flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false);
        ended.next();
        auth.getUserId.mockReturnValue(null);
        response.error(new Error('late failure'));
        expect(await completion).toBe('failed');
        expect(stored).toBeNull();
    });

    it('clears an expired attempt when the profile resumes', () => {
        storeLegacyAttempt();
        vi.advanceTimersByTime(ATTEMPT_TTL_MS);
        expect(flow.consumeError()).toBeNull();
        expect(stored).toBeNull();
    });
});

function storeLegacyAttempt(): void {
    stored = { userId: 'user-1', state, expiresAt: Date.now() + ATTEMPT_TTL_MS, errorKey: null };
}

beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    stored = null;
    auth.getUserId.mockReturnValue('user-1');
    auth.completeTelegramUnlink.mockReturnValue(of(void 0));
    TestBed.configureTestingModule({
        providers: [
            { provide: AuthService, useValue: auth },
            { provide: SessionEventsService, useValue: { sessionEnded$: ended, authenticated$: authenticated } },
            {
                provide: BrowserStorageService,
                useValue: {
                    getJson: (): unknown => stored,
                    setJson: (_scope: string, _key: string, value: unknown): void => {
                        stored = value;
                    },
                    removeItem: (): void => {
                        stored = null;
                    },
                },
            },
        ],
    });
    flow = TestBed.inject(TelegramUnlinkFlowService);
});
afterEach(() => {
    vi.useRealTimers();
});

describe('TelegramUnlinkFlowService', () => {
    it('completes a previously stored unlink attempt once', async () => {
        storeLegacyAttempt();
        expect(await flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false)).toBe('completed');
        expect(auth.completeTelegramUnlink).toHaveBeenCalledWith('code', state);
        expect(stored).toBeNull();
        expect(await flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false)).toBe('unhandled');
    });

    it.each(['cancelled', 'missing-code', 'expired', 'account', 'logout'])('rejects %s without disconnecting', async scenario => {
        storeLegacyAttempt();
        if (scenario === 'expired') {
            vi.advanceTimersByTime(ATTEMPT_TTL_MS);
        }
        if (scenario === 'account') {
            auth.getUserId.mockReturnValue('user-2');
        }
        if (scenario === 'logout') {
            ended.next();
        }
        const result = await flow.handleCallbackAsync(
            scenario === 'missing-code' ? null : telegramOAuthCode('code'),
            telegramOAuthState(state),
            scenario === 'cancelled',
        );
        expect(result).toBe(scenario === 'logout' ? 'unhandled' : 'failed');
        expect(auth.completeTelegramUnlink).not.toHaveBeenCalled();
        if (scenario !== 'account' && scenario !== 'logout') {
            expect(flow.consumeError()).toBe(
                scenario === 'cancelled' ? 'USER_MANAGE.TELEGRAM_UNLINK_CANCELLED' : 'USER_MANAGE.TELEGRAM_UNLINK_ERROR',
            );
            expect(flow.consumeError()).toBeNull();
        }
    });

    it('keeps provider failures visible for a retry in the profile', async () => {
        storeLegacyAttempt();
        auth.completeTelegramUnlink.mockReturnValue(throwError(() => new Error('rejected')));
        expect(await flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false)).toBe('failed');
        expect(flow.consumeError()).toBe('USER_MANAGE.TELEGRAM_UNLINK_ERROR');
    });

    it('does not end a replacement account session when an old completion arrives', async () => {
        storeLegacyAttempt();
        const response = new Subject<void>();
        auth.completeTelegramUnlink.mockReturnValue(response);
        const completion = flow.handleCallbackAsync(telegramOAuthCode('code'), telegramOAuthState(state), false);
        auth.getUserId.mockReturnValue('user-2');
        response.next();
        expect(await completion).toBe('failed');
        expect(flow.consumeError()).toBeNull();
    });
});
