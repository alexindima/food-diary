import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { type DashboardSnapshot } from '../models/dashboard.data';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
    let service: DashboardService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.dashboard;

    const mockSnapshot: DashboardSnapshot = {
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

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should get snapshot with date param', () => {
        const date = new Date('2026-03-15T00:00:00.000Z');

        service.getSnapshot(date).subscribe(result => {
            expect(result).toEqual(mockSnapshot);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === `${baseUrl}/` &&
                r.params.get('date') === date.toISOString() &&
                r.params.get('page') === '1' &&
                r.params.get('pageSize') === '10',
        );
        expect(req.request.method).toBe('GET');
        req.flush(mockSnapshot);
    });

    it('should include optional params', () => {
        const date = new Date('2026-03-15T00:00:00.000Z');

        service.getSnapshot(date, 2, 20, 'en', 7).subscribe(result => {
            expect(result).toEqual(mockSnapshot);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === `${baseUrl}/` &&
                r.params.get('date') === date.toISOString() &&
                r.params.get('page') === '2' &&
                r.params.get('pageSize') === '20' &&
                r.params.get('locale') === 'en' &&
                r.params.get('trendDays') === '7',
        );
        expect(req.request.method).toBe('GET');
        req.flush(mockSnapshot);
    });

    it('should return null on error', () => {
        const date = new Date('2026-03-15T00:00:00.000Z');

        service.getSnapshot(date).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/`);
        req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should mark silent snapshot requests to skip global loading', () => {
        const date = new Date('2026-03-15T00:00:00.000Z');

        service.getSnapshotSilently(date).subscribe();

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/`);
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush({});
    });
});
