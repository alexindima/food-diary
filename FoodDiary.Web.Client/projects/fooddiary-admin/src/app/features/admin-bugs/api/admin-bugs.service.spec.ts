import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { expect, it } from 'vitest';

import type { AdminBugJournalPageHttpResponse } from '../../../shared/api/sdk/generated/model/admin-bug-journal-page-http-response';
import { AdminBugsService } from './admin-bugs.service';

it('forwards journal filters and decodes the latest audit alongside the returned findings', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(AdminBugsService);
    const http = TestBed.inject(HttpTestingController);
    const fromUtc = '2026-10-11T00:00:00Z';
    const toUtc = '2026-10-12T00:00:00Z';
    const startedAtUtc = '2026-10-12T00:04:00Z';
    const result = firstValueFrom(service.getPage({ page: 2, limit: 25, source: 'logs', search: 'Navigation', fromUtc, toUtc }));
    const request = http.expectOne(item => item.method === 'GET' && item.url === 'http://localhost:5300/api/v1/admin/bugs/journal');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('limit')).toBe('25');
    expect(request.request.params.get('source')).toBe('logs');
    expect(request.request.params.get('search')).toBe('Navigation');
    expect(request.request.params.get('fromUtc')).toBe(fromUtc);
    expect(request.request.params.get('toUtc')).toBe(toUtc);
    const response: AdminBugJournalPageHttpResponse = {
        items: [
            {
                id: 'report-id',
                subject: 'Navigation finding',
                receivedAtUtc: startedAtUtc,
                attempt: 1,
                status: 'draft_ready',
                contentExpired: false,
                source: 'logs',
                auditRunId: 'run',
                auditFromUtc: fromUtc,
                auditToUtc: toUtc,
            },
        ],
        isConfigured: true,
        totalItems: 26,
        latestAudit: {
            id: 'run',
            fromUtc,
            toUtc,
            startedAtUtc,
            status: 'running',
            findingCount: 1,
            contentExpired: false,
            isStale: false,
        },
    };
    request.flush(response);

    const page = await result;
    expect(page.totalItems).toBe(response.totalItems);
    expect(page.items).toMatchObject([{ id: 'report-id', source: 'logs', auditRunId: 'run', sourceMessageId: null, title: null }]);
    expect(page.latestAudit).toMatchObject({ id: 'run', status: 'running', completedAtUtc: null, lastSuccessfulAtUtc: null });
    http.verify();
});
