import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { calendarDate } from '../../../shared/models/semantics/date-value';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../models/weekly-goal.data';
import { WeeklyGoalService } from './weekly-goal.service';

const BASE_URL = environment.apiUrls.weeklyGoals;
const GOAL: WeeklyGoal = {
    id: '2ad73d24-e0a5-49a8-a794-bd015e16ef71',
    weekStart: calendarDate('2026-08-17'),
    type: 'DiaryLogging',
    targetDays: 5,
    progressDays: 2,
    isCompleted: false,
    reminderEnabled: true,
    reminderTime: '21:00',
    timeZoneOffsetMinutes: 240,
};

let service: WeeklyGoalService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [WeeklyGoalService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(WeeklyGoalService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('WeeklyGoalService', () => {
    it.each(['2024-02-29', '2025-12-29', '2026-03-09'])('sends calendar date %s without timezone conversion', weekStart => {
        service.getGoal(calendarDate(weekStart)).subscribe();
        const request = httpMock.expectOne(`${BASE_URL}?weekStart=${weekStart}`);
        expect(request.request.method).toBe('GET');
        request.flush(null);
    });

    it('retains the SDK calendar encoding in the decoded goal', () => {
        const encodedWeek = '2026-08-17T00:00:00.0000000Z';
        service.getGoal(calendarDate('2026-08-17')).subscribe(goal => {
            expect(goal?.weekStart).toBe(encodedWeek);
        });
        httpMock.expectOne(`${BASE_URL}?weekStart=2026-08-17`).flush({ ...GOAL, weekStart: encodedWeek });
    });

    it('preserves the distinction between an absent goal and a failed request', () => {
        let result: WeeklyGoal | null | undefined;
        let receivedError: unknown;
        service.getGoal(calendarDate('2026-08-17')).subscribe({
            next: goal => {
                result = goal;
            },
            error: (error: unknown) => {
                receivedError = error;
            },
        });
        httpMock.expectOne(`${BASE_URL}?weekStart=2026-08-17`).flush('Unavailable', {
            status: 503,
            statusText: 'Service Unavailable',
        });
        expect(result).toBeUndefined();
        expect(receivedError).toMatchObject({ status: 503 });
    });

    it('returns null when the requested week has no goal', () => {
        let result: WeeklyGoal | null | undefined;
        service.getGoal(calendarDate('2026-08-17')).subscribe(goal => {
            result = goal;
        });
        httpMock.expectOne(`${BASE_URL}?weekStart=2026-08-17`).flush(null);
        expect(result).toBeNull();
    });

    it('gets a goal for the requested week', () => {
        service.getGoal(calendarDate('2026-08-17')).subscribe(goal => {
            expect(goal).toEqual(GOAL);
        });

        const request = httpMock.expectOne(`${BASE_URL}?weekStart=2026-08-17`);
        expect(request.request.method).toBe('GET');
        request.flush(GOAL);
    });

    it('upserts a weekly goal', () => {
        const payload: UpsertWeeklyGoalPayload = {
            weekStart: calendarDate('2026-08-17'),
            targetDays: 5,
            reminderEnabled: true,
            reminderTime: '21:00',
            timeZoneOffsetMinutes: 240,
        };

        service.upsertGoal(payload).subscribe(goal => {
            expect(goal).toEqual(GOAL);
        });

        const request = httpMock.expectOne(BASE_URL);
        expect(request.request.method).toBe('PUT');
        expect(request.request.body).toEqual(payload);
        request.flush(GOAL);
    });
});
