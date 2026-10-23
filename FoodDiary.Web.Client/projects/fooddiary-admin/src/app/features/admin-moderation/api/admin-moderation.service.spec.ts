import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { AdminContentReportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-content-report-http-response';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminModerationService } from './admin-moderation.service';

const PAGE_SIZE = 20;
const TOTAL_ITEMS = 23;
const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation`;
const report = {
    id: 'report-1',
    reporterId: 'reporter-1',
    targetType: 'Recipe',
    targetId: 'recipe/one + two',
    reason: 'Incorrect ingredients',
    status: 'Reviewed',
    adminNote: 'Confirmed against the recipe',
    createdAtUtc: '2026-10-20T12:30:00Z',
    reviewedAtUtc: '2026-10-21T13:45:00Z',
    reviewedByUserId: 'reviewer-1',
    targetTitle: 'Rice & vegetables',
    targetExcerpt: 'Ingredients to review',
} satisfies AdminContentReportHttpResponse;
const reportPage = { data: [report], page: 2, limit: PAGE_SIZE, totalPages: 2, totalItems: TOTAL_ITEMS };
const operations = [
    { operation: 'reviewReport', path: 'review' },
    { operation: 'dismissReport', path: 'dismiss' },
] as const;

let service: AdminModerationService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [AdminModerationService, provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminModerationService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('AdminModerationService report filters', () => {
    it('serializes pagination, status and target filters and maps the server page', async () => {
        const filters = {
            fromUtc: '2026-10-20T00:00:00Z',
            toUtc: '2026-10-22T00:00:00Z',
            targetType: 'Recipe',
            reporterId: 'reporter-1',
            targetId: report.targetId,
            unused: '',
        };
        const result = firstValueFrom(service.getReports(2, PAGE_SIZE, 'Reviewed', filters));
        const request = httpMock.expectOne(req => req.url === baseUrl);

        expect(request.request.method).toBe('GET');
        expect(Object.fromEntries(request.request.params.keys().map(key => [key, request.request.params.getAll(key)]))).toEqual({
            page: ['2'],
            limit: ['20'],
            status: ['Reviewed'],
            fromUtc: [filters.fromUtc],
            toUtc: [filters.toUtc],
            targetType: ['Recipe'],
            reporterId: ['reporter-1'],
            targetId: ['recipe/one + two'],
        });
        request.flush(reportPage);

        await expect(result).resolves.toEqual({ items: [report], totalPages: 2, totalItems: TOTAL_ITEMS });
    });

    it.each([undefined, null, '', '   '])('omits an absent or blank status (%j) and empty filters', async status => {
        const result = firstValueFrom(service.getReports(1, PAGE_SIZE, status, { targetType: '', reporterId: '', targetId: '' }));
        const request = httpMock.expectOne(req => req.url === baseUrl);

        expect(request.request.method).toBe('GET');
        expect(request.request.params.keys().sort()).toEqual(['limit', 'page']);
        expect(request.request.params.get('page')).toBe('1');
        expect(request.request.params.get('limit')).toBe('20');
        request.flush({ data: [], page: 1, limit: PAGE_SIZE, totalPages: 0, totalItems: 0 });

        await expect(result).resolves.toEqual({ items: [], totalPages: 0, totalItems: 0 });
    });
});

describe('AdminModerationService response boundaries', () => {
    it.each([undefined, null])('preserves missing or null review metadata (%j)', async value => {
        const pendingReport = { ...report, status: 'Pending', reviewedAtUtc: value, reviewedByUserId: value };
        const result = firstValueFrom(service.getReports(2, PAGE_SIZE));
        httpMock.expectOne(req => req.url === baseUrl).flush({ ...reportPage, data: [pendingReport] });

        await expect(result).resolves.toEqual({ items: [pendingReport], totalPages: 2, totalItems: TOTAL_ITEMS });
    });

    it.each([
        { response: { ...reportPage, totalItems: undefined }, field: 'totalItems' },
        { response: { ...reportPage, data: [{ ...report, reporterId: undefined }] }, field: 'reporterId' },
    ])('rejects a response missing $field instead of inventing a value', async ({ response, field }) => {
        const result = firstValueFrom(service.getReports(2, PAGE_SIZE));
        httpMock.expectOne(req => req.url === baseUrl).flush(response);

        await expect(result).rejects.toThrow(`API response is missing required field: ${field}.`);
    });

    it('preserves a report-loading HTTP failure for the page to handle', async () => {
        const payload = { error: 'Reports.Unavailable', message: 'Try again later' };
        const result = firstValueFrom(service.getReports(1, PAGE_SIZE));
        httpMock
            .expectOne(req => req.url === baseUrl)
            .flush(payload, {
                status: HttpStatusCode.ServiceUnavailable,
                statusText: 'Service Unavailable',
            });

        await expect(result).rejects.toMatchObject({ status: HttpStatusCode.ServiceUnavailable, error: payload });
    });
});

describe.each(operations)('AdminModerationService $operation', ({ operation, path }) => {
    it.each([{}, { adminNote: null }, { adminNote: '  Reviewed & retained exactly  ' }])(
        'posts the exact action payload %j to the selected report and completes on 204',
        async action => {
            const result = firstValueFrom(service[operation](adminId<'content-report'>(report.id), action));
            const request = httpMock.expectOne(`${baseUrl}/${report.id}/${path}`);

            expect(request.request.method).toBe('POST');
            expect(request.request.body).toEqual(action);
            expect(request.request.params.keys()).toEqual([]);
            request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

            await expect(result).resolves.toBeNull();
        },
    );

    it('propagates a rejected action without treating it as a successful review', async () => {
        const payload = { error: 'Reports.AlreadyReviewed', message: 'The report has already changed' };
        const result = firstValueFrom(service[operation](adminId<'content-report'>(report.id), { adminNote: 'Retain this note' }));
        httpMock.expectOne(`${baseUrl}/${report.id}/${path}`).flush(payload, {
            status: HttpStatusCode.Conflict,
            statusText: 'Conflict',
        });

        await expect(result).rejects.toMatchObject({ status: HttpStatusCode.Conflict, error: payload });
    });
});
