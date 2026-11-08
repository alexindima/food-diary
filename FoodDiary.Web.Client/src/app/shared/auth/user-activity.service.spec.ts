import { DOCUMENT } from '@angular/common';
import { ApplicationRef, computed, PLATFORM_ID, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { UserService } from '../api/user.service';
import { BrowserSessionStateService } from './browser-session-state.service';
import { UserActivityService } from './user-activity.service';

const interval = 300_000;
const initialNow = 1_000_000;
let now: number;
let page: Document;
let visible: boolean;
const session = signal<{ id: string; impersonating: boolean } | null>(null);
const contextId = signal<string | null>(null);
const recordActivity = vi.fn(() => of(undefined));

beforeEach(() => {
    now = initialNow;
    visible = true;
    session.set({ id: 'first-user', impersonating: false });
    contextId.set(null);
    recordActivity.mockReset().mockReturnValue(of(undefined));
    vi.spyOn(Date, 'now').mockImplementation(() => now);
    TestBed.configureTestingModule({
        providers: [
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: computed(() => session() !== null),
                    isImpersonating: computed(() => session()?.impersonating ?? false),
                    getUserId: (): string | null => session()?.id ?? null,
                },
            },
            { provide: UserService, useValue: { recordActivity } },
            { provide: BrowserSessionStateService, useValue: { contextId } },
        ],
    });
    page = TestBed.inject(DOCUMENT);
    vi.spyOn(page, 'visibilityState', 'get').mockImplementation(() => (visible ? 'visible' : 'hidden'));
});

afterEach(() => {
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
});

async function startAsync(): Promise<void> {
    TestBed.inject(UserActivityService).initialize();
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('foreground user activity', () => {
    it('records an authenticated opening and throttles real interactions without a timer', async () => {
        await startAsync();
        expect(recordActivity).toHaveBeenCalledTimes(1);
        page.dispatchEvent(new Event('pointerdown'));
        now += interval - 1;
        page.dispatchEvent(new Event('keydown'));
        expect(recordActivity).toHaveBeenCalledTimes(1);
        now += 1;
        await TestBed.inject(ApplicationRef).whenStable();
        expect(recordActivity).toHaveBeenCalledTimes(1);
        page.dispatchEvent(new Event('wheel'));
        expect(recordActivity).toHaveBeenCalledTimes(2);
    });

    it('ignores hidden-tab events and records a return to the visible tab', async () => {
        visible = false;
        await startAsync();
        page.dispatchEvent(new Event('pointerdown'));
        page.dispatchEvent(new Event('visibilitychange'));
        expect(recordActivity).not.toHaveBeenCalled();
        visible = true;
        page.dispatchEvent(new Event('visibilitychange'));
        expect(recordActivity).toHaveBeenCalledTimes(1);
        visible = false;
        now += interval;
        page.dispatchEvent(new Event('keydown'));
        expect(recordActivity).toHaveBeenCalledTimes(1);
    });

    it('does not count same-account session refresh, idle time or programmatic scrolling', async () => {
        await startAsync();
        now += interval;
        session.set({ id: 'first-user', impersonating: false });
        page.dispatchEvent(new Event('scroll'));
        await TestBed.inject(ApplicationRef).whenStable();
        expect(recordActivity).toHaveBeenCalledTimes(1);
    });

    it('excludes anonymous and impersonated sessions and resets the throttle when accounts change', async () => {
        session.set(null);
        await startAsync();
        page.dispatchEvent(new Event('pointerdown'));
        session.set({ id: 'support-user', impersonating: true });
        await TestBed.inject(ApplicationRef).whenStable();
        page.dispatchEvent(new Event('keydown'));
        expect(recordActivity).not.toHaveBeenCalled();
        session.set({ id: 'first-user', impersonating: false });
        await TestBed.inject(ApplicationRef).whenStable();
        expect(recordActivity).toHaveBeenCalledTimes(1);
        contextId.set('support-context');
        await TestBed.inject(ApplicationRef).whenStable();
        now += interval;
        page.dispatchEvent(new Event('pointerdown'));
        expect(recordActivity).toHaveBeenCalledTimes(1);
        session.set({ id: 'second-user', impersonating: false });
        contextId.set(null);
        await TestBed.inject(ApplicationRef).whenStable();
        expect(recordActivity).toHaveBeenCalledTimes(2);
    });

    it('handles a failed activity request without background retries or interrupting the user', async () => {
        recordActivity.mockReturnValueOnce(throwError(() => new Error('Offline')));
        await startAsync();
        page.dispatchEvent(new Event('pointerdown'));
        expect(recordActivity).toHaveBeenCalledTimes(1);
        now += interval;
        page.dispatchEvent(new Event('keydown'));
        expect(recordActivity).toHaveBeenCalledTimes(2);
    });
});

describe('user activity during server rendering', () => {
    it('does not register activity during server rendering', async () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                { provide: PLATFORM_ID, useValue: 'server' },
                { provide: AuthService, useValue: {} },
                { provide: UserService, useValue: { recordActivity } },
                { provide: BrowserSessionStateService, useValue: { contextId } },
            ],
        });
        await startAsync();
        page.dispatchEvent(new Event('pointerdown'));
        expect(recordActivity).not.toHaveBeenCalled();
    });
});
