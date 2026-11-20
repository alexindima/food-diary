import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { CurrentLocalDayService } from './current-local-day.service';

const INITIAL_DAY = 11;
const MIDNIGHT_DAY = 12;
const FOLLOWING_DAY = 13;
const MILLISECONDS_PER_SECOND = 1000;
const MILLISECONDS_PER_DAY = 86_400_000;

describe('CurrentLocalDayService', () => {
    afterEach(() => {
        TestBed.resetTestingModule();
        vi.useRealTimers();
    });

    it('advances at local midnight and schedules the next day', () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-10-11T23:59:59'));
        const clock = TestBed.inject(CurrentLocalDayService);
        expect(clock.today().getDate()).toBe(INITIAL_DAY);

        vi.advanceTimersByTime(MILLISECONDS_PER_SECOND);
        expect(clock.today().getDate()).toBe(MIDNIGHT_DAY);
        vi.advanceTimersByTime(MILLISECONDS_PER_DAY);
        expect(clock.today().getDate()).toBe(FOLLOWING_DAY);
    });

    it('refreshes after browser suspension and keeps the same signal value within a day', () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-10-11T12:00:00'));
        const clock = TestBed.inject(CurrentLocalDayService);
        const browserDocument = TestBed.inject(DOCUMENT);
        const initialDay = clock.today();

        vi.setSystemTime(new Date('2026-10-11T15:00:00'));
        browserDocument.dispatchEvent(new Event('visibilitychange'));
        expect(clock.today()).toBe(initialDay);
        vi.setSystemTime(new Date('2026-10-13T10:00:00'));
        browserDocument.defaultView?.dispatchEvent(new Event('focus'));
        expect(clock.today().getDate()).toBe(FOLLOWING_DAY);
    });

    it('keeps the server render date without scheduling browser work', () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-10-11T23:59:59'));
        TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        const clock = TestBed.inject(CurrentLocalDayService);
        const renderDay = clock.today();
        expect(renderDay.getDate()).toBe(INITIAL_DAY);
        expect(vi.getTimerCount()).toBe(0);

        vi.advanceTimersByTime(MILLISECONDS_PER_SECOND);
        const document = TestBed.inject(DOCUMENT);
        document.dispatchEvent(new Event('visibilitychange'));
        document.defaultView?.dispatchEvent(new Event('focus'));
        expect(clock.today()).toBe(renderDay);
        expect(vi.getTimerCount()).toBe(0);
    });

    it('releases its timer and resume listeners when destroyed', () => {
        vi.useFakeTimers();
        const clock = TestBed.inject(CurrentLocalDayService);
        const browserDocument = TestBed.inject(DOCUMENT);
        const initialDay = clock.today();
        TestBed.resetTestingModule();

        vi.setSystemTime(new Date('2030-01-01T12:00:00'));
        browserDocument.dispatchEvent(new Event('visibilitychange'));
        expect(clock.today()).toBe(initialDay);
        expect(vi.getTimerCount()).toBe(0);
    });
});
