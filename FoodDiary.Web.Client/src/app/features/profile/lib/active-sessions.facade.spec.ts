import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
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

    it('loads minimized active-session models only when requested', async () => {
        api.getAll.mockReturnValue(of([currentSession, otherSession]));
        await settleLoadAsync();
        expect(api.getAll).not.toHaveBeenCalled();

        facade.load();
        await settleLoadAsync();

        expect(facade.sessions()).toEqual([currentSession, otherSession]);
        expect(facade.isLoading()).toBe(false);
        expect(facade.error()).toBeNull();
    });

    it('removes sessions only after revocation succeeds without reading again', async () => {
        const thirdSession = createSession('third', false);
        const pending = new Subject<void>();
        api.getAll.mockReturnValue(of([currentSession, otherSession, thirdSession]));
        api.revoke.mockReturnValue(pending);
        api.revokeOthers.mockReturnValue(of(undefined));
        facade.load();
        await settleLoadAsync();

        facade.revoke(otherSession.id);
        facade.load();
        TestBed.tick();
        expect(facade.sessions()).toEqual([currentSession, otherSession, thirdSession]);
        pending.next();
        pending.complete();
        expect(facade.sessions()).toEqual([currentSession, thirdSession]);

        facade.revokeOthers();
        expect(facade.sessions()).toEqual([currentSession]);
        expect(facade.revocation()).toEqual({ kind: 'idle' });
        expect(api.getAll).toHaveBeenCalledOnce();
    });

    it('surfaces load and revoke failures without dropping known sessions', async () => {
        api.getAll.mockReturnValue(of([currentSession, otherSession]));
        facade.load();
        await settleLoadAsync();
        expect(facade.sessions()).toEqual([currentSession, otherSession]);

        api.getAll.mockReturnValueOnce(throwError(() => new Error('read failed')));
        facade.load();
        await settleLoadAsync();
        expect(facade.error()).toBe('load');
        expect(facade.sessions()).toEqual([currentSession, otherSession]);

        const pending = new Subject<void>();
        api.revoke.mockReturnValue(pending);
        facade.revoke(otherSession.id);
        expect(facade.error()).toBeNull();
        pending.error(new Error('revoke failed'));

        expect(facade.error()).toBe('revoke');
        expect(facade.sessions()).toEqual([currentSession, otherSession]);
        expect(facade.revocation()).toEqual({ kind: 'idle' });
    });

    it('cancels a pending list read when its owner is destroyed', () => {
        const pending = new Subject<ActiveSession[]>();
        api.getAll.mockReturnValueOnce(pending);
        facade.load();
        TestBed.tick();
        expect(pending.observed).toBe(true);

        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
    });
});

describe('ActiveSessionsFacade recovery', () => {
    const currentSession = createSession('current', true);
    const otherSession = createSession('other', false);
    const api = { getAll: vi.fn(), revoke: vi.fn(), revokeOthers: vi.fn() };
    let facade: ActiveSessionsFacade;

    beforeEach(async () => {
        vi.clearAllMocks();
        api.getAll.mockReturnValue(of([currentSession, otherSession]));
        TestBed.configureTestingModule({ providers: [ActiveSessionsFacade, { provide: ActiveSessionsService, useValue: api }] });
        facade = TestBed.inject(ActiveSessionsFacade);
        facade.load();
        await settleLoadAsync();
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

    it('prevents duplicate list requests while a reload is pending', async () => {
        const pending = new Subject<ActiveSession[]>();
        api.getAll.mockClear().mockReturnValue(pending);
        facade.load();
        facade.load();
        TestBed.tick();
        facade.load();
        TestBed.tick();

        expect(api.getAll).toHaveBeenCalledOnce();
        expect(facade.sessions()).toEqual([currentSession, otherSession]);
        pending.next([currentSession]);
        pending.complete();
        await settleLoadAsync();
        expect(facade.sessions()).toEqual([currentSession]);
        expect(facade.isLoading()).toBe(false);
    });

    it('keeps an opaque session named all separate from the revoke-others action', async () => {
        const opaque = createSession('all', false);
        const pending = new Subject<void>();
        api.getAll.mockReturnValueOnce(of([currentSession, opaque]));
        facade.load();
        await settleLoadAsync();
        api.revoke.mockReturnValueOnce(pending);

        facade.revoke(opaque.id);

        expect(facade.isRevoking(opaque.id)).toBe(true);
        expect(facade.isRevokingOthers()).toBe(false);
        expect(facade.isBusy()).toBe(true);
        facade.revokeOthers();
        expect(api.revokeOthers).not.toHaveBeenCalled();
        pending.next();
        pending.complete();
        expect(facade.sessions()).toEqual([currentSession]);
        expect(facade.revocation()).toEqual({ kind: 'idle' });
    });

    it('marks only revoke-others busy and recovers its state after an error', () => {
        const pending = new Subject<void>();
        api.revokeOthers.mockReturnValueOnce(pending);

        facade.revokeOthers();

        expect(facade.isRevokingOthers()).toBe(true);
        expect(facade.isRevoking(otherSession.id)).toBe(false);
        facade.revoke(otherSession.id);
        expect(api.revoke).not.toHaveBeenCalled();
        pending.error(new Error('offline'));
        expect(facade.revocation()).toEqual({ kind: 'idle' });
        expect(facade.isBusy()).toBe(false);
        expect(facade.sessions()).toEqual([currentSession, otherSession]);
    });
});

async function settleLoadAsync(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
}

function createSession(id: string, isCurrent: boolean): ActiveSession {
    return {
        id: entityId<'refresh-token-session'>(id),
        isCurrent,
        authProvider: 'password',
        browser: 'Chrome',
        operatingSystem: 'Windows',
        deviceType: 'Desktop',
        createdAtUtc: utcInstant('2030-03-28T11:00:00Z'),
        lastActiveAtUtc: utcInstant('2030-03-28T12:00:00Z'),
    };
}
