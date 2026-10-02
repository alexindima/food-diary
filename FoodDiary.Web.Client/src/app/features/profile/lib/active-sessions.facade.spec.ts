import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ActiveSessionsService } from '../api/active-sessions.service';
import type { ActiveSession } from '../models/active-session.model';
import { ActiveSessionsFacade } from './active-sessions.facade';

describe('ActiveSessionsFacade', () => {
    const currentSession = createSession('current', true);
    const otherSession = createSession('other', false);
    const api = {
        getAll: vi.fn(),
        revoke: vi.fn(),
        revokeOthers: vi.fn(),
    };
    let facade: ActiveSessionsFacade;

    beforeEach(() => {
        vi.clearAllMocks();
        TestBed.configureTestingModule({
            providers: [ActiveSessionsFacade, { provide: ActiveSessionsService, useValue: api }],
        });
        facade = TestBed.inject(ActiveSessionsFacade);
    });

    it('loads minimized active-session models', () => {
        api.getAll.mockReturnValue(of([currentSession, otherSession]));

        facade.load();

        expect(facade.sessions()).toEqual([currentSession, otherSession]);
        expect(facade.isLoading()).toBe(false);
        expect(facade.error()).toBeNull();
    });

    it('removes a revoked session and preserves the current session when revoking others', () => {
        api.getAll.mockReturnValue(of([currentSession, otherSession]));
        api.revoke.mockReturnValue(of(undefined));
        api.revokeOthers.mockReturnValue(of(undefined));
        facade.load();

        facade.revoke(otherSession.id);
        expect(facade.sessions()).toEqual([currentSession]);

        facade.sessions.set([currentSession, otherSession]);
        facade.revokeOthers();
        expect(facade.sessions()).toEqual([currentSession]);
        expect(facade.revokingId()).toBeNull();
    });

    it('surfaces load and revoke failures without dropping known sessions', () => {
        api.getAll.mockReturnValue(of([currentSession, otherSession]));
        api.revoke.mockReturnValue(throwError(() => new Error('failed')));
        facade.load();

        facade.revoke(otherSession.id);

        expect(facade.error()).toBe('revoke');
        expect(facade.sessions()).toEqual([currentSession, otherSession]);
        expect(facade.revokingId()).toBeNull();
    });
});

describe('ActiveSessionsFacade recovery', () => {
    const currentSession = createSession('current', true);
    const otherSession = createSession('other', false);
    const api = { getAll: vi.fn(), revoke: vi.fn(), revokeOthers: vi.fn() };
    let facade: ActiveSessionsFacade;

    beforeEach(() => {
        vi.clearAllMocks();
        api.getAll.mockReturnValue(of([currentSession, otherSession]));
        TestBed.configureTestingModule({ providers: [ActiveSessionsFacade, { provide: ActiveSessionsService, useValue: api }] });
        facade = TestBed.inject(ActiveSessionsFacade);
        facade.load();
    });

    it('clears a failed revocation when retry succeeds', () => {
        api.revoke.mockReturnValueOnce(throwError(() => new Error('unavailable'))).mockReturnValueOnce(of(undefined));
        facade.revoke(otherSession.id);
        expect(facade.error()).toBeTruthy();
        expect(facade.sessions()).toEqual([currentSession, otherSession]);

        facade.revoke(otherSession.id);

        expect(facade.error()).toBeFalsy();
        expect(facade.sessions()).toEqual([currentSession]);
    });

    it('clears a failed revoke-others operation when retry succeeds', () => {
        api.revokeOthers.mockReturnValueOnce(throwError(() => new Error('unavailable'))).mockReturnValueOnce(of(undefined));
        facade.revokeOthers();
        expect(facade.error()).toBeTruthy();
        expect(facade.sessions()).toEqual([currentSession, otherSession]);

        facade.revokeOthers();

        expect(facade.error()).toBeFalsy();
        expect(facade.sessions()).toEqual([currentSession]);
    });

    it('prevents duplicate list requests while a reload is pending', () => {
        const pending = new Subject<ActiveSession[]>();
        api.getAll.mockClear().mockReturnValue(pending);
        facade.load();
        facade.load();

        expect(api.getAll).toHaveBeenCalledOnce();
        expect(facade.sessions()).toEqual([currentSession, otherSession]);
        pending.next([currentSession]);
        pending.complete();
        expect(facade.sessions()).toEqual([currentSession]);
        expect(facade.isLoading()).toBe(false);
    });
});

function createSession(id: string, isCurrent: boolean): ActiveSession {
    return {
        id,
        isCurrent,
        authProvider: 'password',
        browser: 'Chrome',
        operatingSystem: 'Windows',
        deviceType: 'Desktop',
        createdAtUtc: '2030-03-28T11:00:00Z',
        lastActiveAtUtc: '2030-03-28T12:00:00Z',
    };
}
