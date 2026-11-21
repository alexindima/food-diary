import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, type Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { GamificationFacade } from '../../lib/gamification.facade';
import type { Badge, GamificationData } from '../../models/gamification.data';
import { GamificationPageComponent } from './gamification-page';

const CURRENT_STREAK = 3;
const LONGEST_STREAK = 12;
const TOTAL_MEALS_LOGGED = 48;
const HEALTH_SCORE = 76;
const WEEKLY_ADHERENCE = 86;
const WEEKLY_ADHERENCE_FRACTION = 0.86;
const BADGES: Badge[] = [{ key: 'streak_3', category: 'streak', threshold: 3, isEarned: true }];
const ROUTE_BADGES: Badge[] = [
    {
        key: 'streak_3',
        category: 'streak',
        threshold: CURRENT_STREAK,
        isEarned: true,
        earnedAtUtc: '2026-05-01T12:00:00Z',
    },
    {
        key: 'meals_12',
        category: 'meals',
        threshold: LONGEST_STREAK,
        isEarned: true,
        earnedAtUtc: '2026-05-02T12:00:00Z',
    },
    { key: 'academy_articles_1', category: 'academy', threshold: 1, isEarned: true, icon: 'school', earnedAtUtc: '2026-05-03T12:00:00Z' },
];
const PROGRESS: GamificationData = {
    currentStreak: CURRENT_STREAK,
    longestStreak: LONGEST_STREAK,
    totalMealsLogged: TOTAL_MEALS_LOGGED,
    healthScore: HEALTH_SCORE,
    weeklyAdherence: WEEKLY_ADHERENCE_FRACTION,
    badges: ROUTE_BADGES,
};

@Component({ selector: 'fd-progress-exit-fixture', template: '<h1>Other page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class ProgressExitFixtureComponent {}

describe('GamificationPageComponent', () => {
    let facade: GamificationFacadeMock;

    beforeEach(() => {
        facade = createFacadeMock();
        TestBed.configureTestingModule({
            imports: [GamificationPageComponent],
            providers: [provideTranslateTesting()],
        }).overrideProvider(GamificationFacade, { useValue: facade });
    });

    it('initializes facade and renders page sections', () => {
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(facade.initialize).toHaveBeenCalled();
        expect(element.textContent).toContain('GAMIFICATION.TITLE');
        expect(element.textContent).toContain(CURRENT_STREAK.toString());
        expect(element.textContent).toContain(HEALTH_SCORE.toString());
        expect(element.textContent).toContain('GAMIFICATION.BADGE_STREAK_3');
    });

    it('renders loading state', () => {
        facade.isLoading.set(true);
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.textContent).toContain('GAMIFICATION.LOADING');
    });

    it('hides achievement metrics on failure and offers retry', () => {
        facade.hasError.set(true);
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.querySelector('[role="alert"]')?.textContent).toContain('GAMIFICATION.LOAD_ERROR');
        expect(element.querySelector('fd-gamification-habit-path')).toBeNull();
        const retry = [...element.querySelectorAll('button')].find(button => button.textContent.includes('ERRORS.RETRY'));
        expect(retry).toBeDefined();
        retry?.click();
        expect(facade.initialize).toHaveBeenCalledTimes(2);

        facade.hasError.set(false);
        fixture.detectChanges();
        expect(element.querySelector('fd-gamification-habit-path')).not.toBeNull();
    });

    it('reopens the completed progress guide through the localized help button', async () => {
        const translate = TestBed.inject(TranslateService);
        const helpLabel = 'Open the progress guide';
        const stepTitle = 'Your meal streak';
        translate.setTranslation('en', { GAMIFICATION: { TOUR: { START_TOUR: helpLabel, STATS_TITLE: stepTitle } } });
        await firstValueFrom(translate.use('en'));
        const help = getElement(createComponent()).querySelector<HTMLButtonElement>('[data-tour-id="gamification-tour-help"] button');
        if (help === null) {
            throw new Error('Expected the progress guide button');
        }
        expect(help.getAttribute('aria-label')).toBe(helpLabel);

        const tour = TestBed.inject(FdTourService);
        help.click();
        expect(tour.activeTour()?.id).toBe('gamification');
        expect(tour.activeStep()).toMatchObject({ id: 'stats', title: stepTitle });
        tour.complete();
        expect(tour.activeTour()).toBeNull();
        help.click();
        expect(tour.activeStep()).toMatchObject({ id: 'stats', title: stepTitle });
        tour.close();
    });
});

describe('Gamification progress route', () => {
    it('cancels the old progress request on departure and renders a fresh response after returning', async () => {
        TestBed.configureTestingModule({
            providers: [
                provideRouter([
                    { path: 'gamification', component: GamificationPageComponent },
                    { path: 'other', component: ProgressExitFixtureComponent },
                ]),
                provideHttpClient(),
                provideHttpClientTesting(),
                provideTranslateTesting(),
            ],
        });
        const http = TestBed.inject(HttpTestingController);
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/gamification', GamificationPageComponent);
        const firstLoad = http.expectOne(request => request.method === 'GET' && request.url === environment.apiUrls.gamification);
        expect(harness.routeNativeElement?.textContent).toContain('GAMIFICATION.LOADING');

        await harness.navigateByUrl('/other', ProgressExitFixtureComponent);
        expect(firstLoad.cancelled).toBe(true);
        expect(harness.routeNativeElement?.textContent).toContain('Other page');

        await harness.navigateByUrl('/gamification', GamificationPageComponent);
        http.expectOne(request => request.method === 'GET' && request.url === environment.apiUrls.gamification).flush({
            currentStreak: CURRENT_STREAK,
            longestStreak: LONGEST_STREAK,
            totalMealsLogged: TOTAL_MEALS_LOGGED,
            healthScore: HEALTH_SCORE,
            weeklyAdherence: WEEKLY_ADHERENCE_FRACTION,
            badges: BADGES,
        });
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(harness.routeNativeElement?.textContent).toContain('GAMIFICATION.BADGE_STREAK_3');
        expect(harness.routeNativeElement?.textContent).not.toContain('GAMIFICATION.LOADING');
        http.verify();
    });
});

function createComponent(): ComponentFixture<GamificationPageComponent> {
    const fixture = TestBed.createComponent(GamificationPageComponent);
    fixture.detectChanges();

    return fixture;
}

describe('Rendered Progress retry and achievement ownership', () => {
    it('recovers the live progress route and dismisses its achievements when leaving', async () => {
        const { harness, http, host } = await renderProgressAsync();
        expectProgressRequest(http).flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        await redrawProgressAsync(harness);
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('GAMIFICATION.LOAD_ERROR');
        expect(host.querySelector('fd-gamification-habit-path')).toBeNull();

        const retry = Array.from(host.querySelectorAll('button')).find(button => button.textContent.trim() === 'ERRORS.RETRY');
        requireElement(retry).click();
        harness.detectChanges();
        expect(host.textContent).toContain('GAMIFICATION.LOADING');
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expectProgressRequest(http).flush(PROGRESS);
        await redrawProgressAsync(harness);
        const viewAll = requireElement(host.querySelector<HTMLButtonElement>('.habit-path__view-all button'));

        viewAll.click();
        await redrawProgressAsync(harness);
        const manuallyClosed = requireElement(document.querySelector('fd-gamification-achievements-dialog'));
        expect(manuallyClosed.querySelectorAll('.achievements-dialog__item')).toHaveLength(ROUTE_BADGES.length);
        requireElement(manuallyClosed.querySelector<HTMLButtonElement>('button[aria-label="COMMON.CLOSE"]')).click();
        await redrawProgressAsync(harness);
        expect(manuallyClosed.isConnected).toBe(false);

        viewAll.click();
        await redrawProgressAsync(harness);
        const leavingDialog = requireElement(document.querySelector('fd-gamification-achievements-dialog'));
        expect(leavingDialog.textContent).toContain('GAMIFICATION.BADGE_ACADEMY_ARTICLES_1');
        await harness.navigateByUrl('/other', ProgressExitFixtureComponent);
        expect(harness.routeNativeElement?.textContent).toContain('Other page');
        expect(leavingDialog.isConnected).toBe(false);
    });

    it('shows the new user’s locked rewards while leaving the recent list empty', async () => {
        const { harness, http, host } = await renderProgressAsync();
        expectProgressRequest(http).flush({
            currentStreak: 0,
            longestStreak: 0,
            totalMealsLogged: 0,
            healthScore: 0,
            weeklyAdherence: 0,
            badges: ROUTE_BADGES.map(badge => ({ ...badge, isEarned: false, earnedAtUtc: null })),
        } satisfies GamificationData);
        await redrawProgressAsync(harness);

        expect(host.querySelector('.habit-path__recent-list')?.textContent).toContain('GAMIFICATION.HABIT_PATH.NO_RECENT');
        expect(host.querySelectorAll('.habit-path__badge')).toHaveLength(ROUTE_BADGES.length);
        expect(host.querySelector('.habit-path__badge--earned')).toBeNull();
        expect(host.querySelector('.habit-path__view-all')).toBeNull();
    });

    it('shows missing badge icons by category and keeps supplied icons in the collection and all achievements', async () => {
        const { harness, http, host } = await renderProgressAsync();
        expectProgressRequest(http).flush(PROGRESS);
        await redrawProgressAsync(harness);

        const streak = ['GAMIFICATION.BADGE_STREAK_3', 'local_fire_department'];
        const meals = ['GAMIFICATION.BADGE_MEALS_12', 'restaurant'];
        const academy = ['GAMIFICATION.BADGE_ACADEMY_ARTICLES_1', 'school'];
        const collection = Array.from(host.querySelectorAll('.habit-path__badge'), badge => [
            badge.querySelector('.habit-path__badge-title')?.textContent.trim(),
            badge.querySelector('.fd-ui-icon__glyph')?.textContent.trim(),
        ]);
        expect(collection).toEqual([streak, meals, academy]);

        requireElement(host.querySelector<HTMLButtonElement>('.habit-path__view-all button')).click();
        await redrawProgressAsync(harness);
        const dialog = requireElement(document.querySelector('fd-gamification-achievements-dialog'));
        const achievements = Array.from(dialog.querySelectorAll('.achievements-dialog__item'), badge => [
            badge.querySelector('.achievements-dialog__copy strong')?.textContent.trim(),
            badge.querySelector('.fd-ui-icon__glyph')?.textContent.trim(),
        ]);
        expect(achievements).toEqual([academy, meals, streak]);
    });
});

async function renderProgressAsync(): Promise<{ host: HTMLElement; harness: RouterTestingHarness; http: HttpTestingController }> {
    vi.stubGlobal('matchMedia', (): Pick<MediaQueryList, 'matches'> => ({ matches: false }));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'gamification', loadChildren: async (): Promise<{ default: Routes }> => import('../../gamification.routes') },
                { path: 'other', component: ProgressExitFixtureComponent },
            ]),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        http.verify();
        vi.unstubAllGlobals();
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/gamification', GamificationPageComponent);
    return { host: requireElement(harness.routeNativeElement), harness, http };
}

function expectProgressRequest(http: HttpTestingController): ReturnType<HttpTestingController['expectOne']> {
    return http.expectOne(request => request.method === 'GET' && request.url === environment.apiUrls.gamification);
}

async function redrawProgressAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function requireElement<T extends Element>(value: T | null | undefined): T {
    if (value === null || value === undefined) {
        throw new Error('Expected the rendered Progress control');
    }
    return value;
}

function getElement(fixture: ComponentFixture<GamificationPageComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

type GamificationFacadeMock = {
    isLoading: ReturnType<typeof signal<boolean>>;
    hasError: ReturnType<typeof signal<boolean>>;
    currentStreak: ReturnType<typeof signal<number>>;
    longestStreak: ReturnType<typeof signal<number>>;
    totalMealsLogged: ReturnType<typeof signal<number>>;
    healthScore: ReturnType<typeof signal<number>>;
    weeklyAdherence: ReturnType<typeof signal<number>>;
    badges: ReturnType<typeof signal<Badge[]>>;
    initialize: ReturnType<typeof vi.fn>;
};

function createFacadeMock(): GamificationFacadeMock {
    return {
        isLoading: signal(false),
        hasError: signal(false),
        currentStreak: signal(CURRENT_STREAK),
        longestStreak: signal(LONGEST_STREAK),
        totalMealsLogged: signal(TOTAL_MEALS_LOGGED),
        healthScore: signal(HEALTH_SCORE),
        weeklyAdherence: signal(WEEKLY_ADHERENCE),
        badges: signal(BADGES),
        initialize: vi.fn(),
    };
}
