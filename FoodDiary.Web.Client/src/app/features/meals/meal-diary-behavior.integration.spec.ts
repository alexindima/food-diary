import { PlatformLocation, registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import localeRu from '@angular/common/locales/ru';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NavigationError, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { filter, firstValueFrom, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AiInputActionBarComponent } from '../../components/shared/ai-input-bar/ai-input-action-bar';
import type { AiInputBarResult } from '../../components/shared/ai-input-bar/ai-input-bar.types';
import { AuthService } from '../../services/auth.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../shared/development/development-tools';
import type { Meal, MealOverview } from '../../shared/models/meal.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { ViewportService } from '../../shared/platform/viewport.service';
import { NutritionDataInvalidationService } from '../../shared/state/nutrition-data-invalidation.service';
import { MealService } from './api/meal.service';
import { MEAL_DEVELOPMENT_TOOLS, type MealDevelopmentScreen } from './contracts/meal-development-tools';
import { MealListComponent } from './pages/list/meal-list';

@Component({ template: '<h1>Meal editor destination</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class MealDestinationPage {}

registerLocaleData(localeRu);

const PAGE_SIZE = 10;
const LAST_PAGE = 2;
const OUT_OF_RANGE_PAGE = 9;
const MIN_CALORIES = 250;
const MAX_CALORIES = 500;
const FILTER_FROM = '2026-05-04';
const FILTER_TO = '2026-05-05';
const DIARY_DATE = '2026-05-04T12:00:00Z';
const CREATED_DATE = '2026-05-04';
const CREATED_TIME = '12:30';
const MEAL = makeMeal('dinner-row', 'Dinner on the selected page', DIARY_DATE);

type DiaryApi = {
    [Method in 'queryOverview' | 'create' | 'deleteById']: Mock<MealService[Method]>;
};

type DiarySetup = {
    api: DiaryApi;
    developmentScreens: Map<string, DevelopmentScreen>;
    legacyScreens: MealDevelopmentScreen[];
    reportMealChange: Mock<NutritionDataInvalidationService['reportMealMutation']>;
    showError: Mock<FdUiToastService['error']>;
    startTour: Mock<FdTourService['start']>;
};

function configureDiary(): DiarySetup {
    const api: DiaryApi = {
        queryOverview: vi.fn<MealService['queryOverview']>().mockReturnValue(of(overview([]))),
        create: vi.fn<MealService['create']>(),
        deleteById: vi.fn<MealService['deleteById']>(),
    };
    const developmentScreens = new Map<string, DevelopmentScreen>();
    const legacyScreens: MealDevelopmentScreen[] = [];
    const reportMealChange = vi.fn<NutritionDataInvalidationService['reportMealMutation']>();
    const showError = vi.fn<FdUiToastService['error']>();
    const startTour = vi.fn<FdTourService['start']>();
    vi.stubGlobal('matchMedia', (media: string) => ({ matches: false, media }));
    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        providers: [
            provideRouter([
                { path: 'meals/add', component: MealDestinationPage },
                { path: 'meals/:id/edit', component: MealDestinationPage },
                { path: 'meals', component: MealListComponent },
            ]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: { isAuthenticated: signal(true), isPremium: signal(false), getUserId: (): null => null } },
            { provide: MealService, useValue: api },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
            { provide: FdUiToastService, useValue: { error: showError } },
            { provide: FdTourService, useValue: { start: startTour } },
            { provide: NutritionDataInvalidationService, useValue: { reportMealMutation: reportMealChange } },
            {
                provide: DEVELOPMENT_SCREENS,
                useValue: {
                    attach: (screen: DevelopmentScreen) => {
                        developmentScreens.set(screen.kind, screen);
                        return (): void => {
                            developmentScreens.delete(screen.kind);
                        };
                    },
                },
            },
            {
                provide: MEAL_DEVELOPMENT_TOOLS,
                useValue: {
                    attach: (screen: MealDevelopmentScreen) => {
                        legacyScreens.push(screen);
                        return (): void => {
                            legacyScreens.splice(legacyScreens.indexOf(screen), 1);
                        };
                    },
                },
            },
        ],
    });
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
    });
    return { api, developmentScreens, legacyScreens, reportMealChange, showError, startTour };
}

function makeMeal(id: string, comment: string, date: string): Meal {
    return {
        id: entityId<'meal'>(id),
        date: utcInstant(date),
        mealType: 'Dinner',
        comment,
        totalCalories: MAX_CALORIES,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [],
        aiSessions: [],
    };
}

function overview(meals: Meal[], page = 1, totalPages = 1): MealOverview {
    return {
        allMeals: { data: meals, page, totalPages, limit: PAGE_SIZE, totalItems: (totalPages - 1) * PAGE_SIZE + meals.length },
        favoriteItems: [],
        favoriteTotalCount: 0,
    };
}

function routeHost(harness: RouterTestingHarness): HTMLElement {
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected a rendered meal diary page');
    }
    return host;
}

function requiredButton(host: ParentNode, selector: string): HTMLButtonElement {
    const button = host.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected a diary button matching ${selector}`);
    }
    return button;
}

function chipByLabel(dialog: HTMLElement, label: string): HTMLButtonElement {
    const chip = Array.from(dialog.querySelectorAll<HTMLButtonElement>('fd-ui-chip-select button')).find(
        candidate => candidate.textContent.trim() === label,
    );
    if (chip === undefined) {
        throw new Error(`Expected the meal filter option ${label}`);
    }
    return chip;
}

function enterFilterNumber(host: HTMLElement, label: string, value: number): void {
    const input = host.querySelector(`input[aria-label="${label}"]`);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`Expected a diary filter control with label ${label}`);
    }
    input.value = String(value);
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function redraw(harness: RouterTestingHarness): void {
    TestBed.tick();
    harness.detectChanges();
}

describe('Routed meal diary pages', () => {
    it('replaces an out-of-range page with the available page while retaining dates and unrelated query parameters', async () => {
        const { api } = configureDiary();
        api.queryOverview
            .mockReturnValueOnce(of(overview([], OUT_OF_RANGE_PAGE, LAST_PAGE)))
            .mockReturnValueOnce(of(overview([MEAL], LAST_PAGE, LAST_PAGE)));
        const router = TestBed.inject(Router);
        const navigate = vi.spyOn(router, 'navigate');
        const harness = await RouterTestingHarness.create(`/meals?page=${OUT_OF_RANGE_PAGE}&dateFrom=${FILTER_FROM}&campaign=summer`);
        await vi.waitFor(() => {
            redraw(harness);
            expect(router.parseUrl(router.url).queryParams).toEqual({ page: String(LAST_PAGE), dateFrom: FILTER_FROM, campaign: 'summer' });
        });
        expect(api.queryOverview.mock.calls.map(([page]) => page)).toEqual([OUT_OF_RANGE_PAGE, LAST_PAGE]);
        expect(api.queryOverview.mock.lastCall?.[2]).toMatchObject({ dateFrom: new Date(`${FILTER_FROM}T00:00:00`).toISOString() });
        expect(navigate).toHaveBeenCalledWith([], expect.objectContaining({ replaceUrl: true, queryParamsHandling: 'merge' }));
        expect(routeHost(harness).textContent).toContain(MEAL.comment);
    });
});

it('replaces the diary URL when deleting the last meal moves the results to page one', async () => {
    stubScrollIntoView();
    const { api } = configureDiary();
    const firstPageLabel = 'Meal remaining on page one';
    const firstPage = Array.from({ length: PAGE_SIZE }, (_, index) =>
        makeMeal(`previous-meal-${index}`, `${firstPageLabel} ${index}`, DIARY_DATE),
    );
    const emptyLastPage = overview([], LAST_PAGE);
    api.deleteById.mockReturnValueOnce(of(void 0));
    api.queryOverview
        .mockReturnValueOnce(of(overview([MEAL], LAST_PAGE, LAST_PAGE)))
        .mockReturnValueOnce(of({ ...emptyLastPage, allMeals: { ...emptyLastPage.allMeals, totalItems: PAGE_SIZE } }))
        .mockReturnValueOnce(of(overview(firstPage)));

    const harness = await RouterTestingHarness.create(`/meals?page=${LAST_PAGE}&dateFrom=${FILTER_FROM}&campaign=summer`);
    const router = TestBed.inject(Router);
    const host = routeHost(harness);
    expect(host.textContent).toContain(MEAL.comment);
    requiredButton(host, 'fd-meal-card .entity-card__open-button').click();
    await vi.dynamicImportSettled();
    await harness.fixture.whenStable();
    redraw(harness);
    clickMealOverlayAction('fd-meal-detail', 'MEAL_DETAIL.DELETE_BUTTON');
    redraw(harness);
    clickMealOverlayAction('fd-confirm-delete-dialog', 'CONFIRM_DELETE.CONFIRM');

    await vi.waitFor(() => {
        redraw(harness);
        expect(host.textContent).toContain(firstPageLabel);
    });
    expect(document.querySelector('fd-meal-detail')).toBeNull();
    expect(host.textContent).not.toContain(MEAL.comment);
    expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(MEAL.id);
    expect(api.queryOverview.mock.calls.map(([page]) => page)).toEqual([LAST_PAGE, LAST_PAGE, 1]);
    await harness.fixture.whenStable();
    expect(router.parseUrl(router.url).queryParams).toEqual({ dateFrom: FILTER_FROM, campaign: 'summer' });
});

function clickMealOverlayAction(selector: string, label: string): void {
    const button = Array.from(document.querySelectorAll<HTMLButtonElement>(`${selector} button`)).find(
        candidate => candidate.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Expected the meal dialog action ${label}`);
    }
    button.click();
}

describe('Routed meal diary search controls', () => {
    it('keeps a reversed calorie range local and applies corrected filters to the first page', async () => {
        const { api } = configureDiary();
        api.queryOverview.mockReturnValueOnce(of(overview([MEAL], LAST_PAGE, LAST_PAGE)));
        const router = TestBed.inject(Router);
        const harness = await RouterTestingHarness.create(`/meals?page=${LAST_PAGE}&dateFrom=${FILTER_FROM}&campaign=summer`);
        requiredButton(routeHost(harness), 'button[aria-label="MEAL_LIST.DATE_FILTER_BUTTON"]').click();
        redraw(harness);
        const dialog = document.querySelector('fd-meal-list-filters-dialog');
        if (!(dialog instanceof HTMLElement)) {
            throw new Error('Expected the real meal diary filter dialog');
        }
        enterFilterNumber(dialog, 'MEAL_LIST.FILTER_CALORIES_FROM', MAX_CALORIES);
        enterFilterNumber(dialog, 'MEAL_LIST.FILTER_CALORIES_TO', MIN_CALORIES);
        requiredButton(dialog, 'button[type="submit"]').click();
        redraw(harness);

        expect(dialog.querySelector('[role="alert"]')?.textContent).toContain('MEAL_LIST.FILTER_CALORIES_ORDER');
        expect(api.queryOverview).toHaveBeenCalledOnce();
        expect(router.parseUrl(router.url).queryParams['page']).toBe(String(LAST_PAGE));
        enterFilterNumber(dialog, 'MEAL_LIST.FILTER_CALORIES_FROM', MIN_CALORIES);
        enterFilterNumber(dialog, 'MEAL_LIST.FILTER_CALORIES_TO', MAX_CALORIES);
        chipByLabel(dialog, 'MEAL_TYPES.DINNER').click();
        requiredButton(dialog, 'button[type="submit"]').click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(router.parseUrl(router.url).queryParams).toEqual({
                dateFrom: FILTER_FROM,
                campaign: 'summer',
                mealTypes: 'DINNER',
                caloriesFrom: String(MIN_CALORIES),
                caloriesTo: String(MAX_CALORIES),
            });
        });
        expect(api.queryOverview).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ mealTypes: 'DINNER', caloriesFrom: MIN_CALORIES, caloriesTo: MAX_CALORIES }),
            expect.objectContaining({ include: false }),
        );
        expect(dialog.isConnected).toBe(false);
        expect(routeHost(harness).querySelector('.meal-list__no-results')).not.toBeNull();
    });

    it('offers a first meal only after the active filter is cleared and opens the add route', async () => {
        const { api } = configureDiary();
        const harness = await RouterTestingHarness.create('/meals?hasImage=false');
        const host = routeHost(harness);
        expect(host.querySelector('.meal-list__no-results')).not.toBeNull();
        expect(host.querySelector('.meal-list__empty-state')).toBeNull();
        requiredButton(host, '.active-filters__reset button').click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(host.querySelector('.meal-list__empty-state')).not.toBeNull();
        });

        expect(host.querySelector('.meal-list__no-results')).toBeNull();
        expect(api.queryOverview).toHaveBeenCalledTimes(2);
        requiredButton(host, '.meal-list__empty-state button').click();
        await vi.waitFor(() => {
            expect(TestBed.inject(Router).url).toBe('/meals/add');
            expect(routeHost(harness).textContent).toContain('Meal editor destination');
        });
    });
});

it('keeps retry actionable when a late diary result follows a rejected filter History write', async () => {
    const { api } = configureDiary();
    const pending = new Subject<MealOverview>();
    const addedComment = 'Dinner found after removing the AI filter';
    const additional = makeMeal('ai-dinner', addedComment, DIARY_DATE);
    const filtered = overview([MEAL, additional]);
    api.queryOverview
        .mockReturnValueOnce(of(overview([MEAL])))
        .mockReturnValueOnce(pending)
        .mockReturnValueOnce(of(filtered));
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { COMMON: { REMOVE_FILTER: 'Remove {{filter}}' } }, true);
    await firstValueFrom(translate.use('en'));

    const harness = await RouterTestingHarness.create('/meals?mealTypes=DINNER&hasAiSession=false&campaign=summer');
    const router = TestBed.inject(Router);
    const originalUrl = router.url;
    const host = routeHost(harness);
    const expectedQuery = { mealTypes: 'DINNER', campaign: 'summer' };
    expect(host.textContent).toContain(MEAL.comment);
    expect(api.queryOverview).toHaveBeenCalledOnce();

    const error = new DOMException('History write temporarily rejected', 'SecurityError');
    const platform = TestBed.inject(PlatformLocation);
    const pushState = platform.pushState.bind(platform);
    const writeHistory = vi.spyOn(platform, 'pushState').mockImplementationOnce((state: unknown, title, url) => {
        const query = router.parseUrl(url).queryParams;
        if (
            query['hasAiSession'] === undefined &&
            query['mealTypes'] === expectedQuery.mealTypes &&
            query['campaign'] === expectedQuery.campaign
        ) {
            throw error;
        }
        pushState(state, title, url);
    });
    onTestFinished(() => {
        writeHistory.mockRestore();
    });
    const navigationFailure = firstValueFrom(router.events.pipe(filter(event => event instanceof NavigationError)));

    requiredButton(host, 'button[aria-label="Remove MEAL_LIST.FILTER_AI_WITHOUT"]').click();
    const rejectedNavigation = await navigationFailure;
    expect(rejectedNavigation.error).toBe(error);
    await harness.fixture.whenStable();
    redraw(harness);
    expect(router.parseUrl(writeHistory.mock.calls[0][2]).queryParams).toEqual(expectedQuery);
    expect(pending.observed).toBe(true);
    expect(router.url).toBe(originalUrl);

    pending.next(filtered);
    pending.complete();
    redraw(harness);
    expect(pending.observed).toBe(false);
    expect(router.url).toBe(originalUrl);
    const updatedRowVisible = host.textContent.includes(addedComment);
    expect(
        host.querySelector('fd-error-state [role="alert"]'),
        `Retry must remain after loading. New result shown ${String(updatedRowVisible)}.`,
    ).not.toBeNull();

    writeHistory.mockRestore();
    const retry = requiredButton(host, 'fd-error-state button');
    expect(retry.disabled).toBe(false);
    retry.click();

    await vi.waitFor(() => {
        redraw(harness);
        expect(router.parseUrl(router.url).queryParams).toEqual(expectedQuery);
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(host.textContent).toContain(addedComment);
    });
    expect(api.queryOverview).toHaveBeenLastCalledWith(
        1,
        PAGE_SIZE,
        expect.objectContaining({ mealTypes: 'DINNER' }),
        expect.objectContaining({ include: false }),
    );
    expect(api.queryOverview.mock.lastCall?.[2]?.hasAiSession).toBeUndefined();
});

describe('Routed meal diary persisted media filters', () => {
    it('opens the saved photo and AI choices and removes only the photo restriction', async () => {
        const { api } = configureDiary();
        api.queryOverview.mockReturnValueOnce(of(overview([MEAL], LAST_PAGE, LAST_PAGE)));
        const router = TestBed.inject(Router);
        const harness = await RouterTestingHarness.create(`/meals?page=${LAST_PAGE}&hasImage=true&hasAiSession=false&campaign=summer`);
        expect(api.queryOverview.mock.lastCall?.[2]).toMatchObject({ hasImage: true, hasAiSession: false });
        requiredButton(routeHost(harness), 'button[aria-label="MEAL_LIST.DATE_FILTER_BUTTON"]').click();
        redraw(harness);
        const dialog = document.querySelector('fd-meal-list-filters-dialog');
        if (!(dialog instanceof HTMLElement)) {
            throw new Error('Expected the real meal diary filter dialog');
        }

        const yesPhotos = mediaFilterChoice(dialog, 'MEAL_LIST.FILTER_HAS_IMAGE', 'MEAL_LIST.FILTER_YES');
        const anyPhotos = mediaFilterChoice(dialog, 'MEAL_LIST.FILTER_HAS_IMAGE', 'MEAL_LIST.FILTER_ANY');
        const noAi = mediaFilterChoice(dialog, 'MEAL_LIST.FILTER_HAS_AI', 'MEAL_LIST.FILTER_NO');
        expect(yesPhotos.getAttribute('aria-checked')).toBe('true');
        expect(noAi.getAttribute('aria-checked')).toBe('true');
        anyPhotos.click();
        redraw(harness);
        expect(anyPhotos.getAttribute('aria-checked')).toBe('true');
        expect(noAi.getAttribute('aria-checked')).toBe('true');
        expect(api.queryOverview).toHaveBeenCalledOnce();

        requiredButton(dialog, 'button[type="submit"]').click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(router.parseUrl(router.url).queryParams).toEqual({ hasAiSession: 'false', campaign: 'summer' });
            expect(dialog.isConnected).toBe(false);
        });
        expect(api.queryOverview).toHaveBeenCalledTimes(2);
        expect(api.queryOverview).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ hasAiSession: false }),
            expect.objectContaining({ include: false }),
        );
        expect(api.queryOverview.mock.lastCall?.[2].hasImage).toBeUndefined();
    });
});

function mediaFilterChoice(dialog: HTMLElement, group: string, label: string): HTMLButtonElement {
    const options = dialog.querySelectorAll<HTMLButtonElement>(`[role="radiogroup"][aria-label="${group}"] button[role="radio"]`);
    const option = Array.from(options).find(button => button.textContent.trim() === label);
    if (option === undefined) {
        throw new Error(`Expected meal filter choice ${label}`);
    }
    return option;
}

describe('Routed meal diary development controls', () => {
    it('reports the current diary, applies local-day filters and opens only a listed meal', async () => {
        const { api, developmentScreens, legacyScreens } = configureDiary();
        api.queryOverview.mockReturnValue(of(overview([MEAL])));
        const harness = await RouterTestingHarness.create('/meals?campaign=summer');
        const screen = developmentScreens.get('meal-diary');
        if (screen?.setFiltersAsync === undefined || screen.openEntityAsync === undefined) {
            throw new Error('Expected the diary filter and open actions to be registered');
        }
        expect(legacyScreens[0].read()).toMatchObject({ kind: 'meal-diary', dateFrom: null, dateTo: null, visibleMealCount: 1 });
        expect(screen.read()).toMatchObject({ page: 1, totalItems: 1, items: [{ id: MEAL.id, date: MEAL.date }], loading: false });

        await screen.setFiltersAsync({ dateFrom: FILTER_FROM, dateTo: FILTER_TO, mealTypes: 'dinner', hasAiSession: false });
        redraw(harness);

        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({
            dateFrom: FILTER_FROM,
            dateTo: FILTER_TO,
            mealTypes: 'DINNER',
            hasAiSession: 'false',
            campaign: 'summer',
        });
        expect(api.queryOverview.mock.lastCall?.[2]).toMatchObject({
            dateFrom: new Date(`${FILTER_FROM}T00:00:00`).toISOString(),
            dateTo: new Date(`${FILTER_TO}T23:59:59.999`).toISOString(),
            mealTypes: 'DINNER',
            hasAiSession: false,
        });
        expect(legacyScreens[0].read()).toMatchObject({ dateFrom: FILTER_FROM, dateTo: FILTER_TO });
        await expect(screen.openEntityAsync('not-in-the-list')).rejects.toThrow('ID is not in the current diary list.');

        await screen.openEntityAsync(MEAL.id);

        expect(router.url).toBe(`/meals/${MEAL.id}/edit`);
        expect(routeHost(harness).textContent).toContain('Meal editor destination');
        expect(developmentScreens.has('meal-diary')).toBe(false);
        expect(legacyScreens).toHaveLength(0);
    });
});

describe('Routed diary recognition save', () => {
    it('keeps existing rows after a failed recognition save and refreshes the same filter after retry', async () => {
        stubScrollIntoView();
        const { api, reportMealChange, showError } = configureDiary();
        const recognized = makeMeal('new-entry', 'The saved recognition result', DIARY_DATE);
        const pending = new Subject<Meal>();
        api.queryOverview.mockReturnValueOnce(of(overview([MEAL]))).mockReturnValueOnce(of(overview([recognized, MEAL])));
        api.create.mockReturnValueOnce(throwError(() => new Error('Create unavailable'))).mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create('/meals?mealTypes=DINNER');
        for (const block of await harness.fixture.getDeferBlocks()) {
            await block.render(DeferBlockState.Complete);
        }
        redraw(harness);
        const bar = harness.fixture.debugElement.query(By.directive(AiInputActionBarComponent))
            .componentInstance as AiInputActionBarComponent;
        const result: AiInputBarResult = {
            source: 'Text',
            mealType: 'DINNER',
            date: CREATED_DATE,
            time: CREATED_TIME,
            recognizedAtUtc: DIARY_DATE,
            items: [{ nameEn: 'Soup', amount: 250, unit: 'g', calories: 400, proteins: 20, fats: 10, carbs: 40, fiber: 3, alcohol: 0 }],
        };

        bar.mealCreateRequested.emit(result);
        redraw(harness);
        expect(showError).toHaveBeenCalledExactlyOnceWith('AI_INPUT_BAR.CREATE_MEAL_ERROR');
        expect(api.queryOverview).toHaveBeenCalledOnce();
        expect(routeHost(harness).textContent).toContain(MEAL.comment);

        bar.mealCreateRequested.emit(result);
        redraw(harness);
        expect(pending.observed).toBe(true);
        expect(bar.isProcessing()).toBe(true);
        expect(api.create).toHaveBeenCalledTimes(2);
        pending.next(recognized);
        pending.complete();
        redraw(harness);

        expect(reportMealChange).toHaveBeenCalledOnce();
        expect(bar.isProcessing()).toBe(false);
        expect(api.queryOverview.mock.lastCall?.[2]).toMatchObject({ mealTypes: 'DINNER' });
        expect(routeHost(harness).textContent).toContain(recognized.comment);
    });
});

describe('Routed meal diary localization', () => {
    it('orders planned days and translates an already rendered group without reloading or starting the tour', async () => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date('2026-05-01T00:00:00Z'));
        onTestFinished(() => {
            vi.useRealTimers();
        });
        const { api, startTour } = configureDiary();
        const later = makeMeal('later', 'Later planned dinner', '2026-05-06T12:00:00Z');
        api.queryOverview.mockReturnValueOnce(of(overview([later, MEAL])));
        const translate = TestBed.inject(TranslateService);
        await firstValueFrom(translate.use('en'));
        const harness = await RouterTestingHarness.create('/meals');
        const host = routeHost(harness);
        const cards = Array.from(host.querySelectorAll('fd-meal-list-planned fd-meal-card'), card => card.textContent);
        expect(cards).toHaveLength(2);
        expect(cards[0]).toContain(MEAL.comment);
        expect(cards[1]).toContain(later.comment);
        expect(host.querySelector('fd-meal-list-planned')?.textContent).toContain('May');
        expect(startTour).not.toHaveBeenCalled();

        await firstValueFrom(translate.use('ru'));
        redraw(harness);

        expect(host.querySelector('fd-meal-list-planned')?.textContent).toContain('мая');
        expect(api.queryOverview).toHaveBeenCalledOnce();
        requiredButton(host, '[data-tour-id="meal-list-tour-help"] button').click();
        expect(startTour).toHaveBeenCalledExactlyOnceWith(expect.any(Object), { force: true });
    });
});
