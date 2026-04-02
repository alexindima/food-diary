import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminAiUsageService } from './admin-ai-usage.service';
import { environment } from '../../../../environments/environment';

describe('AdminAiUsageService', () => {
    let service: AdminAiUsageService;
    let httpMock: HttpTestingController;

    const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/ai-usage/summary`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminAiUsageService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminAiUsageService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should request AI usage summary', () => {
        const summary = {
            totalTokens: 1000,
            inputTokens: 600,
            outputTokens: 400,
            byDay: [],
            byOperation: [],
            byModel: [],
            byUser: [],
        };

        service.getSummary().subscribe(result => {
            expect(result).toEqual(summary);
        });

        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('GET');
        req.flush(summary);
    });
});
