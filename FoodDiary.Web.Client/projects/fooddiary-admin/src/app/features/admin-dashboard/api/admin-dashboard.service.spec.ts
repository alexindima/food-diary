import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminDashboardService } from './admin-dashboard.service';
import { environment } from '../../../../environments/environment';

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
            recentUsers: [],
        };

        service.getSummary().subscribe(result => {
            expect(result).toEqual(summary);
        });

        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('GET');
        req.flush(summary);
    });
});
