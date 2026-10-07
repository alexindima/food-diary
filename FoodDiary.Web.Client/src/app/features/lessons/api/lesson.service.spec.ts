import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { LessonDetail, LessonPage, LessonSummary } from '../models/lesson.data';
import { LessonService } from './lesson.service';

describe('LessonService', () => {
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

        service.getById('lesson-1').subscribe(result => {
            expect(result).toEqual(lesson);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}/lesson-1`);
        expect(request.request.method).toBe('GET');
        request.flush(lesson);
    });

    it('marks lesson as read', () => {
        service.markRead('lesson-1').subscribe(result => {
            expect(result).toBeNull();
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}/lesson-1/read`);
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toBeNull();
        request.flush(null);
    });
});

function createSummary(): LessonSummary {
    return {
        id: 'lesson-1',
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
