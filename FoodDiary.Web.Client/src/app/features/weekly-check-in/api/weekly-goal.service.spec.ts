import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../models/weekly-goal.data';
import { WeeklyGoalService } from './weekly-goal.service';

const BASE_URL = environment.apiUrls.weeklyGoals;
const GOAL: WeeklyGoal = {
    id: '2ad73d24-e0a5-49a8-a794-bd015e16ef71',
    weekStart: '2026-08-17',
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
    it('gets a goal for the requested week', () => {
        service.getGoal('2026-08-17').subscribe(goal => {
            expect(goal).toEqual(GOAL);
        });

        const request = httpMock.expectOne(`${BASE_URL}/?weekStart=2026-08-17`);
        expect(request.request.method).toBe('GET');
        request.flush(GOAL);
    });

    it('upserts a weekly goal', () => {
        const payload: UpsertWeeklyGoalPayload = {
            weekStart: '2026-08-17',
            targetDays: 5,
            reminderEnabled: true,
            reminderTime: '21:00',
            timeZoneOffsetMinutes: 240,
        };

        service.upsertGoal(payload).subscribe(goal => {
            expect(goal).toEqual(GOAL);
        });

        const request = httpMock.expectOne(`${BASE_URL}/`);
        expect(request.request.method).toBe('PUT');
        expect(request.request.body).toEqual(payload);
        request.flush(GOAL);
    });
});
