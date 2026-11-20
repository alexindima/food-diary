import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { LessonDetail, LessonPage, LessonSummary } from '../models/lesson.data';
import { LessonService } from './lesson.service';

const LEGACY_LESSON_COUNT = 21;
let service: LessonService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [LessonService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LessonService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('LessonService', () => {
    it('loads lessons with locale and optional category', () => {
        const page = createPage();

        service
            .getAll({
                locale: 'en',
                category: ' NutritionBasics ',
                difficulty: 'Beginner',
                search: ' protein ',
                sort: 'shortest',
                page: 2,
                pageSize: 20,
            })
            .subscribe(result => {
                expect(result).toEqual(page);
            });

        const request = httpMock.expectOne(
            `${environment.apiUrls.lessons}?locale=en&category=nutritionbasics&difficulty=beginner&search=protein&sort=shortest&page=2&limit=20`,
        );
        expect(request.request.method).toBe('GET');
        request.flush(page);
    });

    it('propagates load failures instead of inventing an empty catalogue', () => {
        let failed = false;
        service.getAll({ locale: 'en', sort: 'recommended', page: 1, pageSize: 20 }).subscribe({
            next: () => expect.fail('Failed loading must not return an empty catalogue'),
            error: (error: unknown) => {
                failed = true;
                expect(error).toBeDefined();
            },
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}?locale=en&sort=recommended&page=1&limit=20`);
        request.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
        expect(failed).toBe(true);
    });

    it('adapts the legacy array response during a rolling deployment', () => {
        const lessons = Array.from({ length: LEGACY_LESSON_COUNT }, (_, index) => ({ ...createSummary(), id: `lesson-${index + 1}` }));

        service.getAll({ locale: 'en', sort: 'recommended', page: 2, pageSize: 20 }).subscribe(result => {
            expect(result.items.map(lesson => lesson.id)).toEqual(['lesson-21']);
            expect(result.totalCount).toBe(LEGACY_LESSON_COUNT);
            expect(result.totalPages).toBe(2);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}?locale=en&sort=recommended&page=2&limit=20`);
        request.flush(lessons);
    });

    it('loads lesson detail by id', () => {
        const lesson = createDetail();

        service.getById(entityId<'lesson'>('lesson-1')).subscribe(result => {
            expect(result).toEqual(lesson);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}/lesson-1`);
        expect(request.request.method).toBe('GET');
        request.flush(lesson);
    });

    it('marks lesson as read', () => {
        service.markRead(entityId<'lesson'>('lesson-1')).subscribe(result => {
            expect(result).toBeNull();
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}/lesson-1/read`);
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toBeNull();
        request.flush(null);
    });
});

describe('LessonService detail and read failures', () => {
    it('reports a deleted lesson as a not-found response', async () => {
        const id = entityId<'lesson'>('archived-lesson');
        const pending = firstValueFrom(service.getById(id));
        const error = { title: 'Lesson not found' };
        httpMock.expectOne({ method: 'GET', url: `${environment.apiUrls.lessons}/${id}` }).flush(error, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error });
    });

    it('reports a failed read update and succeeds on a new request for the same lesson', async () => {
        const id = entityId<'lesson'>('lesson-1');
        const url = `${environment.apiUrls.lessons}/${id}/read`;
        const failed = firstValueFrom(service.markRead(id));
        httpMock.expectOne({ method: 'POST', url }).flush(null, {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });
        await expect(failed).rejects.toMatchObject({ status: HttpStatusCode.ServiceUnavailable });

        const retry = firstValueFrom(service.markRead(id));
        httpMock.expectOne({ method: 'POST', url }).flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await expect(retry).resolves.toBeNull();
    });
});

function createSummary(): LessonSummary {
    return {
        id: entityId<'lesson'>('lesson-1'),
        title: 'Macros',
        summary: 'Macro basics',
        category: 'Macronutrients',
        difficulty: 'Beginner',
        estimatedReadMinutes: 5,
        isRead: false,
    };
}

function createPage(): LessonPage {
    return {
        items: [createSummary()],
        page: 2,
        pageSize: 20,
        totalCount: 25,
        totalPages: 2,
        totalLessonCount: 31,
        readLessonCount: 6,
        availableCategories: ['Macronutrients'],
    };
}

function createDetail(): LessonDetail {
    return {
        ...createSummary(),
        content: 'Lesson content',
    };
}
