import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

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

describe('AdminDashboardService', () => {
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

    it('lets the API choose its default reporting period when no bounds are supplied', () => {
        const receive = vi.fn();
        service.getOverview({}).subscribe(receive);

        const request = httpMock.expectOne(`${baseUrl}/overview`);
        expect(request.request.params.keys()).toEqual([]);
        request.flush(overview);

        expect(receive).toHaveBeenCalledExactlyOnceWith(overview);
    });

    it('preserves monthly revenue by currency, including the previous period', () => {
        const revenue = [
            { currency: 'USD', gross: 12.34 },
            { currency: 'EUR', gross: 0 },
        ];
        const period = {
            ...overview.period,
            metrics: {
                registrations: 2,
                payingUsers: 1,
                aiTokens: 73,
                trend: [{ date: '2026-08-01', registrations: 2, aiTokens: 73, revenue }],
            },
            currencies: [{ currency: 'USD', gross: 12.34, net: 10.34, refunds: 2, chargebacks: 0, successfulPayments: 1 }],
        };
        const previous = { ...overview.period, fromUtc: '2026-07-01T00:00:00Z', toUtc: period.fromUtc };
        const response = { ...overview, interval: 'month', period, previous };
        const receive = vi.fn();
        service.getOverview({ allTime: true }).subscribe(receive);

        httpMock.expectOne(`${baseUrl}/overview?allTime=true`).flush(response);

        expect(receive).toHaveBeenCalledExactlyOnceWith(response);
    });

    it('preserves a recent user with withheld email', () => {
        const user = { id: 'owned-user', isActive: false, createdOnUtc: '2026-08-12T09:00:00Z', roles: ['User'] };
        const response = { totalUsers: 1, activeUsers: 0, premiumUsers: 0, deletedUsers: 0, pendingReportsCount: 0, recentUsers: [user] };
        const receive = vi.fn();
        service.getSummary().subscribe(receive);

        httpMock.expectOne(baseUrl).flush(response);

        expect(receive).toHaveBeenCalledExactlyOnceWith({ ...response, recentUsers: [{ ...user, email: null }] });
    });
});
