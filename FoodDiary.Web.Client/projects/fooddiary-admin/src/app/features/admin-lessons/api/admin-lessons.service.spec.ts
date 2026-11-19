import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, retry } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { COLLECTION_PAGE_SIZE } from '../../../shared/api/load-paged-collection';
import type { AdminLessonsImportRequest } from '../models/admin-lesson.data';
import { AdminLessonsService } from './admin-lessons.service';

const draft = {
    title: 'Plan a balanced meal',
    content: 'Lesson content',
    summary: null,
    locale: 'en',
    category: 'NutritionBasics',
    difficulty: 'Beginner',
    estimatedReadMinutes: 3,
    sortOrder: 0,
    isPublished: false,
};
const draftResponse = { ...draft, id: 'lesson-1', createdOnUtc: '2026-10-01T12:00:00Z' };

describe('AdminLessonsService requests', () => {
    let lessons: AdminLessonsService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        lessons = TestBed.inject(AdminLessonsService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
    });

    it('reads two pages and preserves older lessons without an update timestamp', async () => {
        const loaded = firstValueFrom(lessons.getAll());
        const first = http.expectOne(request => request.method === 'GET' && request.url.endsWith('/admin/lessons'));
        expect(first.request.params.get('page')).toBe('1');
        expect(first.request.params.get('limit')).toBe(String(COLLECTION_PAGE_SIZE));
        const firstPage = Array.from({ length: COLLECTION_PAGE_SIZE }, (_, index) => ({ ...draftResponse, id: `lesson-${index}` }));
        first.flush(firstPage);

        const next = http.expectOne(request => request.method === 'GET' && request.url.endsWith('/admin/lessons'));
        expect(next.request.params.get('page')).toBe('2');
        const updated = {
            ...draftResponse,
            id: 'lesson-updated',
            summary: 'Short summary',
            modifiedOnUtc: '2026-10-10T12:00:00Z',
            isPublished: true,
        };
        next.flush([updated]);

        expect(await loaded).toEqual([...firstPage.map(item => ({ ...item, modifiedOnUtc: null })), updated]);
    });

    it('keeps the same JSON and idempotency key when retrying the lesson import', async () => {
        const payload: AdminLessonsImportRequest = { version: 1, lessons: [draft] };
        const imported = firstValueFrom(lessons.importLessons(payload).pipe(retry(1)));
        const first = http.expectOne(request => request.method === 'POST' && request.url.endsWith('/admin/lessons/import'));
        const idempotencyKey = first.request.headers.get('Idempotency-Key');
        expect(idempotencyKey).toBeTruthy();
        expect(first.request.body).toEqual(payload);
        first.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });

        const retried = http.expectOne(request => request.method === 'POST' && request.url.endsWith('/admin/lessons/import'));
        expect(retried.request.headers.get('Idempotency-Key')).toBe(idempotencyKey);
        expect(retried.request.body).toEqual(payload);
        retried.flush({ importedCount: 1, lessons: [draftResponse] });

        expect(await imported).toEqual({ importedCount: 1, lessons: [{ ...draftResponse, modifiedOnUtc: null }] });
    });
});
