import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { FastingService } from './fasting.service';

const BASE_URL = environment.apiUrls.fasting;
const SESSION_RESPONSE = {
    id: 'session-1',
    startedAtUtc: '2026-04-12T06:00:00Z',
    endedAtUtc: null,
    initialPlannedDurationHours: 16,
    addedDurationHours: 0,
    plannedDurationHours: 16,
    protocol: 'F16_8',
    planType: 'Intermittent',
    occurrenceKind: 'FastingWindow',
    cyclicFastDays: null,
    cyclicEatDays: null,
    cyclicEatDayFastHours: null,
    cyclicEatDayEatingWindowHours: null,
    cyclicPhaseDayNumber: null,
    cyclicPhaseDayTotal: null,
    isCompleted: false,
    status: 'Active',
    notes: null,
    checkInAtUtc: '2026-04-12T10:00:00Z',
    hungerLevel: 2,
    energyLevel: 4,
    moodLevel: 4,
    symptoms: ['weakness'],
    checkInNotes: 'steady',
    checkIns: [],
};

let service: FastingService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [FastingService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(FastingService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('FastingService overview', () => {
    it('should request fasting overview', () => {
        const overview = createOverview();

        service.getOverview().subscribe(result => {
            expect(result).toEqual(overview);
        });

        const req = httpMock.expectOne(`${BASE_URL}/overview`);
        expect(req.request.method).toBe('GET');
        req.flush(overview);
    });

    it('should fallback overview on error', () => {
        service.getOverview().subscribe(result => {
            expect(result.currentSession).toBeNull();
            expect(result.stats.totalCompleted).toBe(0);
            expect(result.history.data).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/overview`);
        req.flush({ message: 'failed' }, { status: 500, statusText: 'Server Error' });
    });
});

describe('FastingService history', () => {
    it('should request paged fasting history with query params', () => {
        const payload = {
            data: [],
            page: 2,
            limit: 10,
            totalPages: 3,
            totalItems: 21,
        };

        service.getHistory({ from: '2026-04-01T00:00:00.000Z', to: '2026-04-30T23:59:59.000Z', page: 2, limit: 10 }).subscribe(result => {
            expect(result).toEqual(payload);
        });

        const req = httpMock.expectOne(
            request =>
                request.url === `${BASE_URL}/history` &&
                request.params.get('from') === '2026-04-01T00:00:00.000Z' &&
                request.params.get('to') === '2026-04-30T23:59:59.000Z' &&
                request.params.get('page') === '2' &&
                request.params.get('limit') === '10',
        );
        expect(req.request.method).toBe('GET');
        req.flush(payload);
    });
});

describe('FastingService current session', () => {
    it('should send check-in update to current/check-in', () => {
        const payload = {
            hungerLevel: 2,
            energyLevel: 4,
            moodLevel: 4,
            symptoms: ['weakness'],
            checkInNotes: 'steady',
        };

        service.updateCheckIn(payload).subscribe(result => {
            expect(result).toEqual(SESSION_RESPONSE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/current/check-in`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(SESSION_RESPONSE);
    });

    it('should send reduce target request to current/duration/reduce', () => {
        const response = {
            ...SESSION_RESPONSE,
            initialPlannedDurationHours: 36,
            addedDurationHours: -8,
            plannedDurationHours: 28,
            protocol: 'F36_0',
            planType: 'Extended',
            occurrenceKind: 'FastDay',
            checkInAtUtc: null,
            hungerLevel: null,
            energyLevel: null,
            moodLevel: null,
            symptoms: [],
            checkInNotes: null,
        };

        service.reduceTarget({ reducedHours: 8 }).subscribe(result => {
            expect(result).toEqual(response);
        });

        const req = httpMock.expectOne(`${BASE_URL}/current/duration/reduce`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ reducedHours: 8 });
        req.flush(response);
    });
});

function createOverview(): {
    currentSession: null;
    history: {
        data: never[];
        limit: number;
        page: number;
        totalItems: number;
        totalPages: number;
    };
    insights: {
        alerts: never[];
        insights: never[];
    };
    stats: {
        averageDurationHours: number;
        checkInRateLast30Days: number;
        completionRateLast30Days: number;
        currentStreak: number;
        lastCheckInAtUtc: string;
        topSymptom: string;
        totalCompleted: number;
    };
} {
    return {
        currentSession: null,
        stats: {
            totalCompleted: 1,
            currentStreak: 1,
            averageDurationHours: 16,
            completionRateLast30Days: 100,
            checkInRateLast30Days: 50,
            lastCheckInAtUtc: '2026-04-12T10:00:00Z',
            topSymptom: 'headache',
        },
        insights: {
            alerts: [],
            insights: [],
        },
        history: {
            data: [],
            page: 1,
            limit: 10,
            totalPages: 0,
            totalItems: 0,
        },
    };
}
