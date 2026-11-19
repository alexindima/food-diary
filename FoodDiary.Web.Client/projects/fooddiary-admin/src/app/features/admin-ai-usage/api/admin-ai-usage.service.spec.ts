import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AdminAiUsageService } from './admin-ai-usage.service';

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

    it('decodes all breakdowns and preserves a user whose email is unavailable', async () => {
        const from = '2026-10-06';
        const to = '2026-10-07';
        const userId = 'user-1';
        const counts = { totalTokens: 1, inputTokens: 1, outputTokens: 0 };
        const summary = {
            ...counts,
            byDay: [{ ...counts, date: from }],
            byOperation: [{ ...counts, key: 'image-recognition' }],
            byModel: [{ ...counts, key: 'model-one' }],
            byUser: [
                { ...counts, id: userId },
                { ...counts, id: 'user-2', email: 'reader@example.test' },
            ],
        };
        const result = firstValueFrom(service.getSummary({ from, to, userId }));
        const request = httpMock.expectOne(candidate => candidate.url === baseUrl);
        expect(request.request.params.get('from')).toBe(from);
        expect(request.request.params.get('to')).toBe(to);
        expect(request.request.params.get('userId')).toBe(userId);
        request.flush(summary);
        expect(await result).toEqual({ ...summary, byUser: [{ ...summary.byUser[0], email: null }, summary.byUser[1]] });
    });
});
