import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import type { DashboardSnapshotHttpResponse } from '../../../shared/api/sdk/generated/model/dashboard-snapshot-http-response';
import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import { DashboardService } from './dashboard.service';

const BASE_URL = environment.apiUrls.dashboard;
const TEST_DATE = new Date('2026-03-15T00:00:00.000Z');
const TEST_TIME_ZONE_OFFSET_MINUTES = 240;
const MOCK_SNAPSHOT = {
    date: '2026-03-15',
    dateTo: '2026-03-15',
    dailyGoal: 2200,
    weeklyCalorieGoal: 15400,
    statistics: {
        totalCalories: 2000,
        averageProteins: 120,
        averageFats: 70,
        averageCarbs: 210,
        averageFiber: 25,
    },
    weeklyCalories: [],
    cycleHidden: false,
    weight: {
        latest: null,
        previous: null,
        desiredWeightKg: null,
    },
    waist: {
        latest: null,
        previous: null,
        desiredWaistCm: null,
    },
    meals: {
        items: [],
        total: 0,
    },
} satisfies DashboardSnapshot;

let service: DashboardService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [DashboardService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('DashboardService', () => {
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});

describe('DashboardService snapshot', () => {
    it('should get snapshot with date param', () => {
        service.getSnapshot({ date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES }).subscribe(result => {
            expect(result).toEqual(MOCK_SNAPSHOT);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === BASE_URL &&
                r.params.get('date') === TEST_DATE.toISOString() &&
                r.params.get('timeZoneOffsetMinutes') === String(TEST_TIME_ZONE_OFFSET_MINUTES) &&
                r.params.get('page') === '1' &&
                r.params.get('limit') === '10',
        );
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_SNAPSHOT);
    });

    it('should include optional params', () => {
        service
            .getSnapshot({
                date: TEST_DATE,
                timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES,
                timeZoneId: 'Asia/Tbilisi',
                page: 2,
                pageSize: 20,
                locale: 'en',
                trendDays: 7,
            })
            .subscribe(result => {
                expect(result).toEqual(MOCK_SNAPSHOT);
            });

        const req = httpMock.expectOne(
            r =>
                r.url === BASE_URL &&
                r.params.get('date') === TEST_DATE.toISOString() &&
                r.params.get('timeZoneOffsetMinutes') === String(TEST_TIME_ZONE_OFFSET_MINUTES) &&
                r.params.get('timeZoneId') === 'Asia/Tbilisi' &&
                r.params.get('page') === '2' &&
                r.params.get('limit') === '20' &&
                r.params.get('locale') === 'en' &&
                r.params.get('trendDays') === '7',
        );
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_SNAPSHOT);
    });

    it('should return null on error', () => {
        service.getSnapshot({ date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES }).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(r => r.url === BASE_URL);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should rethrow strict snapshot errors', () => {
        const errorSpy = vi.fn();

        service.getSnapshotStrict({ date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES }).subscribe({ error: errorSpy });

        const req = httpMock.expectOne(r => r.url === BASE_URL);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });

        expect(errorSpy).toHaveBeenCalledTimes(1);
    });
});

describe('DashboardService composed snapshot', () => {
    it('loads measurements, trends, advice and hydration together without losing dates or optional settings', () => {
        const wire = dashboardSnapshotFixture();
        const received = vi.fn<(snapshot: DashboardSnapshot) => void>();
        service.getSnapshotStrict({ date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES }).subscribe(received);
        const request = httpMock.expectOne(candidate => candidate.url === BASE_URL && candidate.method === 'GET');
        request.flush(wire);

        expect(received).toHaveBeenCalledOnce();
        const snapshot = received.mock.lastCall?.[0];
        if (snapshot === undefined) {
            throw new Error('Dashboard snapshot was not emitted');
        }
        expect(snapshot.weight).toEqual(wire.weight);
        expect(snapshot.waist).toEqual(wire.waist);
        expect(snapshot.weightTrend).toEqual(wire.weightTrend);
        expect(snapshot.waistTrend).toEqual(wire.waistTrend);
        expect(snapshot.weeklyCalories).toEqual(wire.weeklyCalories);
        expect(snapshot.advice).toEqual(wire.advice);
        expect(snapshot.hydration).toEqual({ ...wire.hydration, goalMl: null });
        expect(snapshot.hydrationHistory).toEqual(wire.hydrationHistory);
        expect(snapshot.dashboardLayout).toStrictEqual({ web: ['hydration'], mobile: undefined, setupHintDismissed: undefined });
        expect(snapshot.tdeeInsight).toMatchObject({ confidence: 'medium', dataDaysUsed: 14, estimatedTdee: 2150, adaptiveTdee: null });
    });

    it('rejects an incomplete hydration entry and accepts the complete snapshot when retried', () => {
        const wire = dashboardSnapshotFixture();
        const error = vi.fn();
        const received = vi.fn();
        const read = service.getSnapshotStrict({ date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES });
        read.subscribe({ next: received, error });
        httpMock
            .expectOne(candidate => candidate.url === BASE_URL)
            .flush({
                ...wire,
                hydrationHistory: { entries: [{ timestampUtc: '2026-03-15T19:20:00Z', amountMl: 180 }], hasMore: false },
            });
        expect(received).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ message: 'API response is missing required field: id.' }));

        read.subscribe({ next: received, error });
        httpMock.expectOne(candidate => candidate.url === BASE_URL).flush(wire);
        expect(received).toHaveBeenCalledOnce();
        expect(error).toHaveBeenCalledOnce();
    });
});

function dashboardSnapshotFixture(): DashboardSnapshotHttpResponse {
    const earlier = '2026-03-14';
    const today = '2026-03-15';
    return {
        ...MOCK_SNAPSHOT,
        weeklyCalories: [
            { date: earlier, calories: 1850 },
            { date: today, calories: 2000 },
        ],
        weight: {
            latest: { date: today, weightKg: 76.4 },
            previous: { date: earlier, weightKg: 76.9 },
            desiredWeightKg: 74,
        },
        waist: {
            latest: { date: today, circumferenceCm: 89 },
            previous: { date: earlier, circumferenceCm: 90 },
            desiredWaistCm: 86,
        },
        weightTrend: [{ startDate: earlier, endDate: today, averageWeightKg: 76.7 }],
        waistTrend: [{ startDate: earlier, endDate: today, averageCircumferenceCm: 89.5 }],
        advice: { id: 'advice', locale: 'en', weight: 1, value: 'Keep a water bottle nearby.' },
        hydration: { dateUtc: today, totalMl: 900 },
        hydrationHistory: {
            entries: [{ id: 'water', timestampUtc: '2026-03-15T22:15:00+03:00', amountMl: 300 }],
            hasMore: false,
        },
        tdeeInsight: { confidence: 'medium', dataDaysUsed: 14, estimatedTdee: 2150 },
        dashboardLayout: { web: ['hydration'], mobile: null, setupHintDismissed: null },
    };
}

describe('DashboardService silent snapshot', () => {
    it('should mark silent snapshot requests to skip global loading', () => {
        service.getSnapshotSilently({ date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES }).subscribe();

        const req = httpMock.expectOne(r => r.url === BASE_URL);
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush({});
    });

    it('should mark strict silent snapshot requests to skip global loading', () => {
        service.getSnapshotSilentlyStrict({ date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES }).subscribe();

        const req = httpMock.expectOne(r => r.url === BASE_URL);
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush(MOCK_SNAPSHOT);
    });
});

describe('DashboardService silent failures', () => {
    it.each([false, true])('preserves silent request context on failure (strict=%s)', strict => {
        const next = vi.fn();
        const error = vi.fn();
        const query = { date: TEST_DATE, timeZoneOffsetMinutes: TEST_TIME_ZONE_OFFSET_MINUTES, locale: '  ' };
        const request = strict ? service.getSnapshotSilentlyStrict(query) : service.getSnapshotSilently(query);
        request.subscribe({ next, error });
        const req = httpMock.expectOne(r => r.url === BASE_URL);
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        expect(req.request.params.has('locale')).toBe(false);
        expect(req.request.params.has('trendDays')).toBe(false);
        req.flush('offline', { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        if (strict) {
            expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: HttpStatusCode.ServiceUnavailable }));
            expect(next).not.toHaveBeenCalled();
        } else {
            expect(next).toHaveBeenCalledWith(null);
            expect(error).not.toHaveBeenCalled();
        }
    });
});
