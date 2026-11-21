import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import type { FoodRecognitionJobHttpResponse } from '../../shared/api/sdk/generated/model/food-recognition-job-http-response';
import type { FoodRecognitionJobHttpResponsePagedHttpResponse } from '../../shared/api/sdk/generated/model/food-recognition-job-http-response-paged-http-response';
import { UserFacade } from '../../shared/lib/user.facade';
import { ViewportService } from '../../shared/platform/viewport.service';
import { PrivacyConsentFacade } from '../../shared/privacy/privacy-consent.facade';
import mealsRoutes from './meals.routes';

vi.mock('@microsoft/signalr', () => ({
    LogLevel: { None: 0 },
    HubConnectionBuilder: class {
        public withUrl(): this {
            return this;
        }

        public withAutomaticReconnect(): this {
            return this;
        }

        public configureLogging(): this {
            return this;
        }

        public build(): object {
            return {
                on: vi.fn(),
                onreconnected: vi.fn(),
                onclose: vi.fn(),
                start: vi.fn().mockRejectedValue(new Error('Realtime unavailable in the history fixture')),
            };
        }
    },
}));

const JOBS_URL = `${environment.apiUrls.ai}/food/recognitions`;
const NUTRITION_URL = `${environment.apiUrls.ai}/food/nutrition`;
const HISTORY_TRIGGER = 'button[aria-label="AI_RECOGNITION.SHOW_RECENT"]';
const HISTORY_ROW = 'fd-food-recognition-history-dialog .history-list__open';
const REVIEW = 'fd-ai-photo-result';
const ANALYZING = 'MEAL_MANAGE.PHOTO_AI_DIALOG.STATUS_ANALYZING';
const NUTRITION_ERROR = 'MEAL_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR';
const RECOGNIZED_FOOD = 'Lentil soup';
const RUNNING_JOB = {
    id: 'f173c999-7ab3-40c7-8acf-f05bfa26cbd4',
    imageAssetId: 'd7318b1c-451e-4c0d-8ce8-b1dd4724ef2a',
    imageUrl: '/fixtures/meal-photo.png',
    description: null,
    isProductLabel: false,
    status: 'Running',
    createdOnUtc: '2026-11-20T12:00:00Z',
    updatedOnUtc: '2026-11-20T12:00:01Z',
    vision: null,
    nutrition: null,
    errorCode: null,
    nutritionErrorCode: null,
} satisfies FoodRecognitionJobHttpResponse;
const JOB_WITHOUT_NUTRITION = {
    ...RUNNING_JOB,
    status: 'Succeeded',
    updatedOnUtc: '2026-11-20T12:00:02Z',
    vision: { items: [{ nameEn: RECOGNIZED_FOOD, amount: 200, unit: 'g', confidence: 1 }] },
    nutritionErrorCode: 'Ai.OpenAiFailed',
} satisfies FoodRecognitionJobHttpResponse;

it('keeps recognized food visible when a resumed history job cannot calculate nutrition', async () => {
    const http = configureDiaryHistory();
    const harness = await RouterTestingHarness.create('/meals');
    const diary = await vi.waitFor(() =>
        http.expectOne(request => request.method === 'GET' && request.url === `${environment.apiUrls.meals}/overview`),
    );
    diary.flush({
        allMeals: { data: [], page: 1, limit: 10, totalPages: 1, totalItems: 0 },
        favoriteItems: [],
        favoriteTotalCount: 0,
    });
    for (const block of await harness.fixture.getDeferBlocks()) {
        await block.render(DeferBlockState.Complete);
    }
    harness.detectChanges();
    if (harness.routeNativeElement === null) {
        throw new Error('Expected the rendered meal diary');
    }

    requireButton(harness.routeNativeElement, HISTORY_TRIGGER).click();
    const history = await vi.waitFor(() => http.expectOne(request => request.method === 'GET' && request.url === `${JOBS_URL}/page`));
    history.flush({
        data: [RUNNING_JOB],
        page: 1,
        limit: 10,
        totalPages: 1,
        totalItems: 1,
    } satisfies FoodRecognitionJobHttpResponsePagedHttpResponse);
    harness.detectChanges();
    const row = requireButton(document, HISTORY_ROW);
    expect(row.textContent).toContain('AI_RECOGNITION.STATUS.Running');
    row.click();

    const poll = await vi.waitFor(() => http.expectOne({ method: 'GET', url: `${JOBS_URL}/${RUNNING_JOB.id}` }));
    harness.detectChanges();
    const review = document.querySelector(REVIEW);
    if (!(review instanceof HTMLElement)) {
        throw new Error('Expected the history recognition review');
    }
    expect(review.textContent).toContain(ANALYZING);
    poll.flush(JOB_WITHOUT_NUTRITION);

    await vi.waitFor(() => {
        harness.detectChanges();
        expect(review.querySelector('[role="alert"]')?.textContent).toContain(NUTRITION_ERROR);
    });
    expect(review.textContent).toContain(RECOGNIZED_FOOD);
    expect(review.textContent).not.toContain(ANALYZING);
    expect(requireButton(review, '.ai-photo-result__submit-action button').disabled).toBe(true);
    http.expectNone({ method: 'POST', url: JOBS_URL });
    http.expectNone({ method: 'POST', url: NUTRITION_URL });
    http.expectNone({ method: 'POST', url: environment.apiUrls.meals });
});

function configureDiaryHistory(): HttpTestingController {
    vi.stubGlobal('matchMedia', (media: string) => ({ matches: false, media }));
    stubScrollIntoView();
    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        providers: [
            provideRouter([{ path: 'meals', children: mealsRoutes }], withComponentInputBinding()),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: signal(true),
                    isPremium: signal(true),
                    getUserId: (): string => 'meal-history-owner',
                    getToken: (): string => 'fixture-token',
                    sessionHubUrl: (url: string): string => url,
                },
            },
            { provide: UserFacade, useValue: { user: signal({ aiConsentAcceptedAt: '2026-11-20T11:00:00Z' }) } },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
            {
                provide: PrivacyConsentFacade,
                useValue: {
                    ensureHealthConsentAsync: vi.fn<PrivacyConsentFacade['ensureHealthConsentAsync']>().mockResolvedValue(true),
                    failed: signal(false),
                    needsReload: signal(false),
                },
            },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        try {
            http.verify();
        } finally {
            dialogs.closeAll();
            vi.unstubAllGlobals();
        }
    });
    return http;
}

function requireButton(host: ParentNode, selector: string): HTMLButtonElement {
    const button = host.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected button ${selector}`);
    }
    return button;
}
