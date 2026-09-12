import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { SessionEventsService } from './session-events.service';
import { TelegramBackupEmailFlowService } from './telegram-backup-email-flow.service';

describe('TelegramBackupEmailFlowService', () => {
    const stateLength = 43;
    const state = 's'.repeat(stateLength);
    let flow: TelegramBackupEmailFlowService;
    let stored: unknown;
    const ended = new Subject<void>();
    const auth = { getUserId: vi.fn(), startTelegramBackupEmail: vi.fn(), completeTelegramBackupEmail: vi.fn() };
    const browser = { open: vi.fn() };

    beforeEach(() => {
        stored = null;
        vi.clearAllMocks();
        auth.getUserId.mockReturnValue('user-1');
        auth.startTelegramBackupEmail.mockReturnValue(of({ authorizationUrl: `https://oauth.telegram.org/auth?state=${state}` }));
        auth.completeTelegramBackupEmail.mockReturnValue(of(void 0));
        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: auth },
                { provide: BrowserWindowService, useValue: browser },
                { provide: SessionEventsService, useValue: { sessionEnded$: ended } },
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
        flow = TestBed.inject(TelegramBackupEmailFlowService);
    });

    it('returns a successful email request without logging in or completing a normal login intent', async () => {
        await flow.startAsync('backup@example.com');
        expect(browser.open).toHaveBeenCalledOnce();
        expect(await flow.handleCallbackAsync('code', state, false)).toBe(true);
        expect(auth.completeTelegramBackupEmail).toHaveBeenCalledWith('code', state);
        expect(flow.read()?.sentAt).toBeTypeOf('number');
        expect(flow.read()?.state).toBeNull();
        expect(await flow.handleCallbackAsync('code', state, false)).toBe(false);
    });

    it.each(['cancelled', 'wrong-state', 'no-code'])('rejects %s without sending a completion', async scenario => {
        await flow.startAsync('backup@example.com');
        await flow.handleCallbackAsync(
            scenario === 'no-code' ? null : 'code',
            scenario === 'wrong-state' ? 'foreign' : state,
            scenario === 'cancelled',
        );
        expect(auth.completeTelegramBackupEmail).not.toHaveBeenCalled();
        expect(flow.read()?.failed).toBe(true);
        expect(flow.read()?.sentAt).toBeNull();
    });

    it('clears pending data when the signed-in account changes or the session ends', async () => {
        await flow.startAsync('backup@example.com');
        auth.getUserId.mockReturnValue('user-2');
        expect(flow.read()).toBeNull();
        flow.markSent('other@example.com');
        ended.next();
        expect(stored).toBeNull();
    });

    it('does not claim an email was sent when completion fails', async () => {
        await flow.startAsync('backup@example.com');
        auth.completeTelegramBackupEmail.mockReturnValue(throwError(() => new Error('expired')));
        await flow.handleCallbackAsync('code', state, false);
        expect(flow.read()?.failed).toBe(true);
        expect(flow.read()?.sentAt).toBeNull();
    });

    it('rejects a redirect to an unexpected provider', async () => {
        auth.startTelegramBackupEmail.mockReturnValue(of({ authorizationUrl: `https://example.com/auth?state=${state}` }));
        await expect(flow.startAsync('backup@example.com')).rejects.toThrow();
        expect(browser.open).not.toHaveBeenCalled();
    });
});
