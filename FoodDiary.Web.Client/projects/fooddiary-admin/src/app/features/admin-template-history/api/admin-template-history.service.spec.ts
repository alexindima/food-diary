import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { AdminTemplateHistoryService } from './admin-template-history.service';

describe('AdminTemplateHistoryService', () => {
    it.each([
        { kind: 'ai-prompts', key: 'text-parse', locale: 'ru' },
        { kind: 'email-templates', key: 'welcome', locale: 'en' },
    ] as const)('loads $kind revisions for the requested key and locale', async ({ kind, key, locale }) => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        const service = TestBed.inject(AdminTemplateHistoryService);
        const http = TestBed.inject(HttpTestingController);
        const revisions = firstValueFrom(service.getRevisions(kind, key, locale));
        const request = http.expectOne(
            candidate => candidate.method === 'GET' && candidate.url.endsWith(`/admin/${kind}/${key}/${locale}/revisions`),
        );
        const archived = {
            id: 'older-revision',
            textBody: 'Earlier version',
            isActive: false,
            savedOnUtc: '2026-09-01T12:00:00Z',
            archivedOnUtc: '2026-10-01T12:00:00Z',
        };
        const detailed = {
            ...archived,
            id: 'detailed-revision',
            textBody: 'Updated content',
            subject: 'Revised subject',
            htmlBody: '<p>Updated content</p>',
            version: 2,
        };
        request.flush([detailed, archived]);

        expect(await revisions).toEqual([detailed, { ...archived, subject: null, htmlBody: null, version: null }]);
        http.verify();
    });
});
