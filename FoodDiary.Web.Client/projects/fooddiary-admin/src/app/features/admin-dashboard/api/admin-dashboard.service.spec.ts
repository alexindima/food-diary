import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AdminDashboardService } from './admin-dashboard.service';

const overview = {
    fromUtc: '2026-08-01T00:00:00Z',
    toUtc: '2026-09-01T00:00:00Z',
    interval: 'day',
    period: {
        fromUtc: '2026-08-01T00:00:00Z',
        toUtc: '2026-09-01T00:00:00Z',
        metrics: { registrations: 0, payingUsers: 0, aiTokens: 0, trend: [] },
        currencies: [],
    },
    previous: null,
    totalUsersNow: 0,
    premiumUsersNow: 0,
    pendingReportsNow: 0,
};

describe('AdminDashboardService', () => {
    let service: AdminDashboardService;
    let httpMock: HttpTestingController;

    const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/dashboard`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminDashboardService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminDashboardService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should request dashboard summary', () => {
        const summary = {
            totalUsers: 10,
            activeUsers: 8,
            premiumUsers: 3,
            deletedUsers: 2,
            pendingReportsCount: 0,
            recentUsers: [],
        };

        service.getSummary().subscribe(result => {
            expect(result).toEqual(summary);
        });

        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('GET');
        req.flush(summary);
    });

    it('sends inclusive dates to overview', () => {
        service.getOverview({ from: '2026-08-01', to: '2026-08-31' }).subscribe();
        const req = httpMock.expectOne(request => request.url === `${baseUrl}/overview`);
        expect(req.request.params.get('from')).toBe('2026-08-01');
        expect(req.request.params.get('to')).toBe('2026-08-31');
        req.flush(overview);
    });

    it('omits stale date parameters for all time', () => {
        service.getOverview({ allTime: true, from: '2026-08-01' }).subscribe();
        const req = httpMock.expectOne(`${baseUrl}/overview?allTime=true`);
        expect(req.request.params.has('from')).toBe(false);
        req.flush(overview);
    });
});
