import { type HttpRequest, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { LessonDetailHttpResponse } from '../../shared/api/sdk/generated/model/lesson-detail-http-response';
import type { LessonPageHttpResponse } from '../../shared/api/sdk/generated/model/lesson-page-http-response';
import lessonRoutes from './lesson.routes';

const LESSON_ID = '2b4822ad-19bc-4312-a310-9a965c28d569';
const API_PATH = environment.apiUrls.lessons;
const FIRST_PAGE = 1;
const PAGE_SIZE = 18;
const ESTIMATED_MINUTES = 5;
const SUMMARY = {
    id: LESSON_ID,
    title: 'Build a balanced breakfast',
    category: 'NutritionBasics',
    difficulty: 'Beginner',
    estimatedReadMinutes: ESTIMATED_MINUTES,
    isRead: false,
} as const;
const FOLLOWUP = { ...SUMMARY, id: '71311cbd-a52c-4e48-b126-b30d21cef808', title: 'Choose foods that keep you full' };
const DETAIL: LessonDetailHttpResponse = { ...SUMMARY, content: '<p>Start with foods you like.</p>' };
const CATALOGUE: LessonPageHttpResponse = {
    items: [SUMMARY, FOLLOWUP],
    page: FIRST_PAGE,
    pageSize: PAGE_SIZE,
    totalCount: 2,
    totalPages: 1,
    totalLessonCount: 2,
    readLessonCount: 0,
    availableCategories: [SUMMARY.category],
};

describe('Lesson detail requests', () => {
    it('requests only the selected article and reads catalogue progress from the server after Back', async () => {
        const http = setupHttp();
        const harness = await RouterTestingHarness.create(`/lessons/${LESSON_ID}`);
        harness.detectChanges();

        const detail = http.expectOne({ method: 'GET', url: `${API_PATH}/${LESSON_ID}` });
        http.expectNone(isCatalogueRequest);
        detail.flush(DETAIL);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('#lesson-detail-title')?.textContent).toBe(SUMMARY.title);
        });

        clickButton(harness, 'LESSONS.MARK_READ');
        http.expectOne({ method: 'POST', url: `${API_PATH}/${LESSON_ID}/read` }).flush(null);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('.lesson-detail__read-badge')?.textContent).toContain('LESSONS.ALREADY_READ');
        });
        http.expectNone(isCatalogueRequest);

        const back = harness.routeNativeElement?.querySelector('button[aria-label="LESSONS.BACK"]');
        if (!(back instanceof HTMLButtonElement)) {
            throw new Error('Expected the rendered Lesson Back action');
        }
        back.click();
        const catalogue = await vi.waitFor(() => http.expectOne(isCatalogueRequest));
        expect(catalogue.request.urlWithParams).toBe(`${API_PATH}?locale=en&sort=recommended&page=${FIRST_PAGE}&limit=${PAGE_SIZE}`);
        catalogue.flush({ ...CATALOGUE, items: [{ ...SUMMARY, isRead: true }, FOLLOWUP], readLessonCount: 1 });
        await vi.waitFor(() => {
            harness.detectChanges();
            const page = harness.routeNativeElement;
            expect(page?.querySelector('.academy-progress__summary strong')?.textContent.trim()).toBe('1 / 2');
            expect(page?.querySelector('.lessons-list__recommendation')?.textContent).toContain(FOLLOWUP.title);
            const completed = Array.from(page?.querySelectorAll('.lesson-card--read .lesson-card__title') ?? []);
            expect(completed.map(heading => heading.textContent.trim())).toEqual([SUMMARY.title]);
        });
    });

    it('keeps a cold blank bookmark out of the API and opens a valid lesson after retrying a temporary failure', async () => {
        const http = setupHttp();
        const harness = await RouterTestingHarness.create('/lessons/%20');
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toBe('LESSONS.NOT_FOUND');
        });
        http.expectNone(request => request.url.startsWith(API_PATH));

        const detailUrl = `${API_PATH}/${LESSON_ID}`;
        await harness.navigateByUrl(`/lessons/${LESSON_ID}`);
        const first = await vi.waitFor(() => http.expectOne({ method: 'GET', url: detailUrl }));
        first.flush('Service unavailable', { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toBe('LESSONS.DETAIL_LOAD_ERROR');
        });

        clickButton(harness, 'ERRORS.RETRY');
        const retry = await vi.waitFor(() => http.expectOne({ method: 'GET', url: detailUrl }));
        retry.flush(DETAIL);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('#lesson-detail-title')?.textContent).toBe(SUMMARY.title);
            expect(harness.routeNativeElement?.querySelector('[role="alert"]:not([hidden])')).toBeNull();
        });
    });
});

function setupHttp(): HttpTestingController {
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            provideRouter([{ path: 'lessons', children: lessonRoutes }], withComponentInputBinding()),
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        http.verify();
    });
    return http;
}

function clickButton(harness: RouterTestingHarness, label: string): void {
    const buttons = Array.from(harness.routeNativeElement?.querySelectorAll('button') ?? []);
    const button = buttons.find(item => item.querySelector('.fd-ui-button__label')?.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected a rendered button with label ${label}`);
    }
    button.click();
}

function isCatalogueRequest(request: HttpRequest<unknown>): boolean {
    return request.method === 'GET' && request.url === API_PATH;
}
