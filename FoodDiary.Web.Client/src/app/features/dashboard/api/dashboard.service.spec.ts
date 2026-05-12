import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import type { DashboardSnapshot } from '../models/dashboard.data';
import { DashboardService } from './dashboard.service';

const BASE_URL = environment.apiUrls.dashboard;
const TEST_DATE = new Date('2026-03-15T00:00:00.000Z');
const MOCK_SNAPSHOT: DashboardSnapshot = {
    date: '2026-03-15',
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
    weight: {
        latest: null,
        previous: null,
        desired: null,
    },
    waist: {
        latest: null,
        previous: null,
        desired: null,
    },
    meals: {
        items: [],
        total: 0,
    },
};

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
        service.getSnapshot({ date: TEST_DATE }).subscribe(result => {
            expect(result).toEqual(MOCK_SNAPSHOT);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === `${BASE_URL}/` &&
                r.params.get('date') === TEST_DATE.toISOString() &&
                r.params.get('page') === '1' &&
                r.params.get('pageSize') === '10',
        );
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_SNAPSHOT);
    });

    it('should include optional params', () => {
        service.getSnapshot({ date: TEST_DATE, page: 2, pageSize: 20, locale: 'en', trendDays: 7 }).subscribe(result => {
            expect(result).toEqual(MOCK_SNAPSHOT);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === `${BASE_URL}/` &&
                r.params.get('date') === TEST_DATE.toISOString() &&
                r.params.get('page') === '2' &&
                r.params.get('pageSize') === '20' &&
                r.params.get('locale') === 'en' &&
                r.params.get('trendDays') === '7',
        );
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_SNAPSHOT);
    });

    it('should return null on error', () => {
        service.getSnapshot({ date: TEST_DATE }).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('DashboardService silent snapshot', () => {
    it('should mark silent snapshot requests to skip global loading', () => {
        service.getSnapshotSilently({ date: TEST_DATE }).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/`);
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush({});
    });
});
