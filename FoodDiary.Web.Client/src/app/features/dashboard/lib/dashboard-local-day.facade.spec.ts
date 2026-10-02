import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { DashboardLocalDayFacade } from './dashboard-local-day.facade';

const BEFORE_MIDNIGHT = new Date('2026-10-02T23:59:59');
const NEXT_DAY = new Date('2026-10-03T00:00:00');
const MIDNIGHT_DELAY_MS = 1000;

afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
});

describe('Dashboard local day clock', () => {
    it('updates at each local midnight and schedules the following calendar day', () => {
        const service = setup();
        const changed = vi.fn();
        service.changes.subscribe(changed);
        vi.advanceTimersByTime(MIDNIGHT_DELAY_MS);
        expect(service.today()).toEqual(NEXT_DAY);
        expect(changed).toHaveBeenCalledTimes(1);
        const followingDay = new Date(NEXT_DAY);
        followingDay.setDate(followingDay.getDate() + 1);
        vi.advanceTimersByTime(followingDay.getTime() - NEXT_DAY.getTime());
        expect(service.today()).toEqual(followingDay);
        expect(changed).toHaveBeenCalledTimes(2);
    });

    it('catches up on focus when a suspended tab missed the timer', () => {
        const service = setup();
        const doc = TestBed.inject(DOCUMENT);
        vi.setSystemTime(NEXT_DAY);
        doc.defaultView?.dispatchEvent(new Event('focus'));
        expect(service.today()).toEqual(NEXT_DAY);
    });

    it('catches up when the document becomes visible and does not react while hidden', () => {
        const service = setup();
        const doc = TestBed.inject(DOCUMENT);
        const visibility = vi.spyOn(doc, 'visibilityState', 'get').mockReturnValue('hidden');
        vi.setSystemTime(NEXT_DAY);
        doc.dispatchEvent(new Event('visibilitychange'));
        expect(service.today()).toEqual(new Date('2026-10-02T00:00:00'));
        visibility.mockReturnValue('visible');
        doc.dispatchEvent(new Event('visibilitychange'));
        expect(service.today()).toEqual(NEXT_DAY);
        visibility.mockRestore();
    });

    it('does not schedule browser timers on the server', () => {
        setup('server');
        expect(vi.getTimerCount()).toBe(0);
    });

    it('removes its timer and resume listeners on destruction', () => {
        const service = setup();
        const doc = TestBed.inject(DOCUMENT);
        TestBed.resetTestingModule();
        expect(vi.getTimerCount()).toBe(0);
        vi.setSystemTime(NEXT_DAY);
        doc.defaultView?.dispatchEvent(new Event('focus'));
        doc.dispatchEvent(new Event('visibilitychange'));
        expect(service.today()).toEqual(new Date('2026-10-02T00:00:00'));
    });
});

function setup(platform = 'browser'): DashboardLocalDayFacade {
    vi.useFakeTimers();
    vi.setSystemTime(BEFORE_MIDNIGHT);
    TestBed.configureTestingModule({ providers: [DashboardLocalDayFacade, { provide: PLATFORM_ID, useValue: platform }] });
    return TestBed.inject(DashboardLocalDayFacade);
}
