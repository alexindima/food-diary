import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { AdminAuditEntryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-audit-entry-http-response';
import { AdminAuditService } from './admin-audit.service';

const PAGE_SIZE = 25;
const TOTAL_ITEMS = 51;
const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/audit`;
const entry = {
    id: 'audit-1',
    actorUserId: 'admin-1',
    subjectClientUserId: 'client-1',
    action: 'Review & dismiss',
    targetType: 'ContentReport',
    targetId: 'report/one + two',
    metadata: '{"reason":"Reviewed by support"}',
    createdAtUtc: '2026-10-21T13:45:00Z',
} satisfies AdminAuditEntryHttpResponse;

let service: AdminAuditService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [AdminAuditService, provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminAuditService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('AdminAuditService query contracts', () => {
    it('serializes the server page, UTC period and identity/action filters without changing their values', async () => {
        const filters = {
            page: 2,
            limit: PAGE_SIZE,
            fromUtc: '2026-10-20T00:00:00Z',
            toUtc: '2026-10-22T00:00:00Z',
            actorUserId: entry.actorUserId,
            subjectClientUserId: entry.subjectClientUserId,
            action: entry.action,
            targetType: entry.targetType,
            targetId: entry.targetId,
        };
        const result = firstValueFrom(service.getPage(filters));
        const request = httpMock.expectOne(req => req.url === baseUrl);

        expect(request.request.method).toBe('GET');
        expect(Object.fromEntries(request.request.params.keys().map(key => [key, request.request.params.getAll(key)]))).toEqual({
            page: ['2'],
            limit: ['25'],
            fromUtc: ['2026-10-20T00:00:00Z'],
            toUtc: ['2026-10-22T00:00:00Z'],
            actorUserId: ['admin-1'],
            subjectClientUserId: ['client-1'],
            action: ['Review & dismiss'],
            targetType: ['ContentReport'],
            targetId: ['report/one + two'],
        });
        request.flush({ items: [entry], totalItems: TOTAL_ITEMS });

        await expect(result).resolves.toEqual({ items: [entry], totalItems: TOTAL_ITEMS });
    });

    it('omits filters that were not supplied and preserves an empty page with zero total', async () => {
        const result = firstValueFrom(service.getPage({ page: 1, limit: PAGE_SIZE }));
        const request = httpMock.expectOne(req => req.url === baseUrl);

        expect(request.request.method).toBe('GET');
        expect(request.request.params.keys().sort()).toEqual(['limit', 'page']);
        expect(request.request.params.get('page')).toBe('1');
        expect(request.request.params.get('limit')).toBe('25');
        request.flush({ items: [], totalItems: 0 });

        await expect(result).resolves.toEqual({ items: [], totalItems: 0 });
    });
});

describe('AdminAuditService response boundaries', () => {
    it.each([undefined, null])('normalizes missing or null optional audit details (%j)', async value => {
        const responseEntry = { ...entry, subjectClientUserId: value, targetId: value, metadata: value };
        const result = firstValueFrom(service.getPage({ page: 1, limit: PAGE_SIZE }));
        httpMock.expectOne(req => req.url === baseUrl).flush({ items: [responseEntry], totalItems: 1 });

        await expect(result).resolves.toEqual({
            items: [{ ...entry, subjectClientUserId: null, targetId: null, metadata: null }],
            totalItems: 1,
        });
    });

    it.each([
        { response: { totalItems: 0 }, field: 'items' },
        { response: { items: [] }, field: 'totalItems' },
        { response: { items: [{ ...entry, actorUserId: undefined }], totalItems: 1 }, field: 'actorUserId' },
    ])('rejects a response missing $field instead of silently replacing it', async ({ response, field }) => {
        const result = firstValueFrom(service.getPage({ page: 1, limit: PAGE_SIZE }));
        httpMock.expectOne(req => req.url === baseUrl).flush(response);

        await expect(result).rejects.toThrow(`API response is missing required field: ${field}.`);
    });

    it('preserves an HTTP failure for the audit page to display and retry', async () => {
        const payload = { error: 'Audit.Unavailable', message: 'Try again later' };
        const result = firstValueFrom(service.getPage({ page: 1, limit: PAGE_SIZE }));
        httpMock
            .expectOne(req => req.url === baseUrl)
            .flush(payload, {
                status: HttpStatusCode.ServiceUnavailable,
                statusText: 'Service Unavailable',
            });

        await expect(result).rejects.toMatchObject({ status: HttpStatusCode.ServiceUnavailable, error: payload });
    });
});
