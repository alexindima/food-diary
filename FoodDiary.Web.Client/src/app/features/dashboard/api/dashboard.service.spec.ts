import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';
import { environment } from '../../../../environments/environment';

describe('DashboardService', () => {
    let service: DashboardService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.dashboard;

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
        const mockSnapshot = { totalCalories: 2000 };

        service.getSnapshot(date).subscribe(result => {
            expect(result).toEqual(mockSnapshot as any);
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
        const mockSnapshot = { totalCalories: 2000 };

        service.getSnapshot(date, 2, 20, 'en', 7).subscribe(result => {
            expect(result).toEqual(mockSnapshot as any);
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
});
