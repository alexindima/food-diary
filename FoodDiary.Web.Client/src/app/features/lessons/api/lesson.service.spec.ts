import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { LessonDetail, LessonSummary } from '../models/lesson.data';
import { LessonService } from './lesson.service';

describe('LessonService', () => {
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
        const lessons = [createSummary()];

        service.getAll('en', ' NutritionBasics ').subscribe(result => {
            expect(result).toEqual(lessons);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}/?locale=en&category=NutritionBasics`);
        expect(request.request.method).toBe('GET');
        request.flush(lessons);
    });

    it('returns empty list when lesson loading fails', () => {
        service.getAll('en').subscribe(result => {
            expect(result).toEqual([]);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.lessons}/?locale=en`);
        request.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
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
        expect(request.request.body).toEqual({});
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

function createDetail(): LessonDetail {
    return {
        ...createSummary(),
        content: 'Lesson content',
    };
}
