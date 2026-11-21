import { type HttpRequest, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import type { MealHttpResponse } from '../../shared/api/sdk/generated/model/meal-http-response';
import { UserFacade } from '../../shared/lib/user.facade';
import { ViewportService } from '../../shared/platform/viewport.service';
import { PrivacyConsentFacade } from '../../shared/privacy/privacy-consent.facade';
import mealsRoutes from './meals.routes';

const TIME_ZONE = 'America/Los_Angeles';
const CALENDAR_DATE = '2026-03-08';
const NONEXISTENT_TIME = '02:30';
const CORRECTED_TIME = '03:45';
const CORRECTED_INSTANT = '2026-03-08T10:45:00.000Z';
const MEAL_NAME = 'Lentil soup';
const PORTION = 200;
const NUTRIENTS = { calories: 160, protein: 8, fat: 4, carbs: 24, fiber: 6, alcohol: 0 };
const EMPTY_DIARY = { allMeals: { data: [], page: 1, limit: 10, totalPages: 1, totalItems: 0 }, favoriteItems: [], favoriteTotalCount: 0 };

describe('AI meal local time in the diary', () => {
    it('keeps the review when the selected minute does not exist and saves the corrected instant', async () => {
        const { http, showError } = configureDiary();
        const harness = await RouterTestingHarness.create('/meals');
        const review = await recognizeMealAsync(harness, http);
        expandMealDetails(review);
        selectMealDate(review);
        selectMealTime(review, NONEXISTENT_TIME);
        const submit = requireButton(review, '.ai-photo-result__submit-action button');
        expect(submit.disabled).toBe(false);
        expect(timeInput(review).value).toBe(NONEXISTENT_TIME);

        submit.click();
        harness.detectChanges();
        const invalidWrites = http.match(isMealWrite).map((request): unknown => request.request.body);
        expect(invalidWrites).toEqual([]);
        expect(showError).toHaveBeenCalledExactlyOnceWith('MEAL_MANAGE.NONEXISTENT_TIME');
        expect(review.isConnected).toBe(true);
        expect(review.textContent).toContain(MEAL_NAME);
        expect(timeInput(review).value).toBe(NONEXISTENT_TIME);

        selectMealTime(review, CORRECTED_TIME);
        submit.click();
        const request = http.expectOne(isMealWrite);
        expect(request.request.body).toMatchObject({ date: CORRECTED_INSTANT, aiSessions: [{ source: 'Text' }] });
        request.flush(savedMeal());
        harness.detectChanges();
        const refreshedDiary = await vi.waitFor(() => http.expectOne(isDiaryRead));
        refreshedDiary.flush(EMPTY_DIARY);
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('fd-ai-photo-result')).toBeNull();
        http.expectNone(isMealWrite);
    });
});

function configureDiary(): { http: HttpTestingController; showError: ReturnType<typeof vi.fn<FdUiToastService['error']>> } {
    vi.stubEnv('TZ', TIME_ZONE);
    expect(new Intl.DateTimeFormat().resolvedOptions().timeZone).toBe(TIME_ZONE);
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-03-08T08:15:00.000Z'));
    vi.stubGlobal('matchMedia', (media: string) => ({ matches: false, media }));
    stubScrollIntoView();
    const showError = vi.fn<FdUiToastService['error']>();
    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        providers: [
            provideRouter([{ path: 'meals', children: mealsRoutes }], withComponentInputBinding()),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: { isAuthenticated: signal(true), isPremium: signal(true), getUserId: (): null => null } },
            { provide: UserFacade, useValue: { user: signal({ aiConsentAcceptedAt: '2026-03-01T12:00:00Z' }) } },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
            {
                provide: PrivacyConsentFacade,
                useValue: {
                    ensureHealthConsentAsync: vi.fn<PrivacyConsentFacade['ensureHealthConsentAsync']>().mockResolvedValue(true),
                    failed: signal(false),
                    needsReload: signal(false),
                },
            },
            { provide: FdUiToastService, useValue: { error: showError, success: vi.fn() } },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        try {
            http.verify();
        } finally {
            dialogs.closeAll();
            vi.useRealTimers();
            vi.unstubAllEnvs();
            vi.unstubAllGlobals();
        }
    });
    return { http, showError };
}

async function recognizeMealAsync(harness: RouterTestingHarness, http: HttpTestingController): Promise<HTMLElement> {
    const initialDiary = await vi.waitFor(() => http.expectOne(isDiaryRead));
    initialDiary.flush(EMPTY_DIARY);
    for (const block of await harness.fixture.getDeferBlocks()) {
        await block.render(DeferBlockState.Complete);
    }
    harness.detectChanges();
    const diary = harness.routeNativeElement;
    if (diary === null) {
        throw new Error('Expected the meal diary');
    }
    const text = diary.querySelector('.ai-input-bar__input');
    if (!(text instanceof HTMLInputElement)) {
        throw new Error('Expected the meal recognition text field');
    }
    text.value = MEAL_NAME;
    text.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();
    requireButton(diary, 'button[aria-label="AI_INPUT_BAR.ADD_ACTION"]').click();
    const parsing = await vi.waitFor(() => http.expectOne(request => request.url.endsWith('/ai/food/text')));
    expect(parsing.request.body).toEqual({ text: MEAL_NAME });
    parsing.flush({ items: [{ nameEn: MEAL_NAME, amount: PORTION, unit: 'g', confidence: 1 }] });
    http.expectOne(request => request.url.endsWith('/ai/food/nutrition')).flush({
        ...NUTRIENTS,
        items: [{ name: MEAL_NAME, amount: PORTION, unit: 'g', ...NUTRIENTS }],
    });
    harness.detectChanges();
    const review = diary.querySelector('fd-ai-photo-result');
    if (!(review instanceof HTMLElement)) {
        throw new Error('Expected the meal recognition review');
    }
    expect(review.textContent).toContain(MEAL_NAME);
    return review;
}

function expandMealDetails(review: HTMLElement): void {
    requireButton(review, '.ai-photo-result__details-toggle button').click();
    TestBed.tick();
    expect(review.querySelector('.ai-photo-result__details')?.hasAttribute('inert')).toBe(false);
}

function selectMealDate(review: HTMLElement): void {
    requireButton(review, 'fd-ui-date-input button[aria-haspopup="dialog"]').click();
    TestBed.tick();
    requireButton(document, `button[data-date="${CALENDAR_DATE}"]`).click();
    TestBed.tick();
}

function selectMealTime(review: HTMLElement, time: string): void {
    const [hour, minute] = time.split(':');
    timeInput(review).click();
    TestBed.tick();
    timeOption('HOURS', hour).click();
    TestBed.tick();
    timeOption('MINUTES', minute).click();
    TestBed.tick();
}

function timeInput(review: HTMLElement): HTMLInputElement {
    const input = review.querySelector('fd-ui-time-input input[role="combobox"]');
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected the meal time picker');
    }
    return input;
}

function timeOption(part: 'HOURS' | 'MINUTES', value: string): HTMLButtonElement {
    const button = Array.from(document.querySelectorAll<HTMLButtonElement>(`[aria-label="MEAL_DETAILS.${part}"] button`)).find(
        choice => choice.textContent.trim() === value,
    );
    if (button === undefined) {
        throw new Error(`Expected time option ${value}`);
    }
    return button;
}

function requireButton(host: ParentNode, selector: string): HTMLButtonElement {
    const button = host.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected button ${selector}`);
    }
    return button;
}

function isDiaryRead(request: HttpRequest<unknown>): boolean {
    return request.method === 'GET' && request.url.endsWith('/meals/overview');
}

function isMealWrite(request: HttpRequest<unknown>): boolean {
    return request.method === 'POST' && request.url.endsWith('/meals');
}

function savedMeal(): MealHttpResponse {
    return {
        id: '32de1050-da71-4ebb-9e46-7344b563b407',
        date: CORRECTED_INSTANT,
        totalCalories: NUTRIENTS.calories,
        totalProteins: NUTRIENTS.protein,
        totalFats: NUTRIENTS.fat,
        totalCarbs: NUTRIENTS.carbs,
        totalFiber: NUTRIENTS.fiber,
        totalAlcohol: NUTRIENTS.alcohol,
        isNutritionAutoCalculated: true,
        items: [],
    };
}
