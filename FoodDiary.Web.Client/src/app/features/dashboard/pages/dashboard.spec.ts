import { NgTemplateOutlet, registerLocaleData } from '@angular/common';
import ru from '@angular/common/locales/ru';
import { type DebugElement, NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { BehaviorSubject, of } from 'rxjs';
import { describe, expect, it, type MockInstance, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { NavigationService } from '../../../services/navigation.service';
import { UnsavedChangesService } from '../../../services/unsaved-changes.service';
import { ViewportService } from '../../../shared/platform/viewport.service';
import { ThemeService } from '../../../shared/theme/theme.service';
import { LocalizedTourDefinitionService } from '../../../shared/tours/localized-tour-definition.service';
import type { FastingSession } from '../../fasting/models/fasting.data';
import { AiMealCreateFacade } from '../../meals/lib/ai/ai-meal-create.facade';
import { DashboardFacade } from '../lib/dashboard.facade';
import { DashboardLayoutService } from '../lib/dashboard-layout.service';
import { DashboardComponent } from './dashboard';

registerLocaleData(ru);
const WATER_AMOUNT = 250;
const CALORIE_TARGET = 2100;
const ONE_HOUR_MS = 3600000;

// Keep the production page template and its bindings. Child components have their own
// DOM tests; this suite isolates page orchestration from HTTP, overlays and defer timing.
describe('Dashboard page composition', () => {
    it('renders an initial loader without actionable cards, then keeps existing cards during refresh', async () => {
        const { facade, fixture, host } = await setupAsync();
        facade.isLoading.set(true);
        facade.hasSnapshot.set(false);
        fixture.detectChanges();
        expect(host.querySelector('.dashboard__loader')).not.toBeNull();
        expect(host.querySelector('fd-dashboard-summary-block')).toBeNull();
        facade.hasSnapshot.set(true);
        fixture.detectChanges();
        expect(host.querySelector('.dashboard__loader')).toBeNull();
        expect(host.querySelector('fd-dashboard-summary-block')).not.toBeNull();
        expect(host.querySelector('.dashboard__refresh-overlay[role="status"]')).not.toBeNull();
    });

    it.each([true, false])('places exactly one hydration card in the correct column (mobile=%s)', async mobile => {
        const { fixture, host, viewport } = await setupAsync();
        viewport.isMobile.set(mobile);
        fixture.detectChanges();
        expect(host.querySelectorAll('fd-dashboard-hydration-block')).toHaveLength(1);
        const hydration = host.querySelector('fd-dashboard-hydration-block');
        expect(hydration?.closest('.dashboard__column--aside') !== null).toBe(!mobile);
        if (mobile) {
            const main = host.querySelector('.dashboard__column');
            expect(Array.from(main?.children ?? []).map(child => child.tagName)).toEqual(
                expect.arrayContaining(['FD-DASHBOARD-SUMMARY-BLOCK', 'FD-DASHBOARD-HYDRATION-BLOCK', 'FD-DASHBOARD-MEALS-BLOCK']),
            );
            expect(hydration?.nextElementSibling?.tagName).toBe('FD-DASHBOARD-MEALS-BLOCK');
        }
        viewport.isMobile.set(!mobile);
        fixture.detectChanges();
        expect(host.querySelectorAll('fd-dashboard-hydration-block')).toHaveLength(1);
        expect(host.querySelector('fd-dashboard-hydration-block')?.closest('.dashboard__column--aside') !== null).toBe(mobile);
    });

    it('makes a historical date read-only for quick additions and marks TDEE as current', async () => {
        const { facade, fixture, host, viewport, child } = await setupAsync();
        viewport.isMobile.set(true);
        facade.isTodaySelected.set(false);
        fixture.detectChanges();
        expect(host.querySelector('fd-dashboard-quick-add')).toBeNull();
        expect(child('fd-dashboard-hydration-block').properties['canAdd']).toBe(false);
        expect(child('fd-dashboard-tdee-block').properties['isHistorical']).toBe(true);
        expect(child('fd-dashboard-fasting-block').properties['shouldRender']).toBe(false);
        expect(host.querySelector('fd-dashboard-hydration-block')?.closest('.dashboard__column--aside')).not.toBeNull();
        facade.isTodaySelected.set(true);
        fixture.detectChanges();
        expect(host.querySelector('fd-dashboard-quick-add')).not.toBeNull();
        expect(child('fd-dashboard-hydration-block').properties['canAdd']).toBe(true);
        expect(child('fd-dashboard-tdee-block').properties['isHistorical']).toBe(false);
    });

    it('passes the meal count through even when the recorded calories are zero', async () => {
        const { fixture, facade, child } = await setupAsync();
        facade.snapshot.set({ dailyGoal: 2000, meals: { total: 1 } });
        fixture.detectChanges();
        expect(child('fd-dashboard-summary-block').properties['data']).toEqual(expect.objectContaining({ mealCount: 1, dailyConsumed: 0 }));
    });

    it('prioritizes an active fast once and does not jump when refreshed into an eating phase', async () => {
        const { fixture, facade, host } = await setupAsync(true);
        const firstBlock = (): string | undefined => host.querySelector('.dashboard__column')?.firstElementChild?.tagName;
        expect(firstBlock()).toBe('FD-DASHBOARD-FASTING-BLOCK');
        facade.currentFastingSession.set({ ...activeFast(), planType: 'Cyclic', occurrenceKind: 'EatDay' });
        fixture.detectChanges();
        expect(firstBlock()).toBe('FD-DASHBOARD-FASTING-BLOCK');
        expect(host.querySelectorAll('fd-dashboard-fasting-block')).toHaveLength(1);
        facade.isTodaySelected.set(false);
        fixture.detectChanges();
        expect(firstBlock()).toBe('FD-DASHBOARD-SUMMARY-BLOCK');
    });

    it('waits for the initial snapshot before deciding fasting priority', async () => {
        const { fixture, facade, host } = await setupAsync(false, true);
        facade.currentFastingSession.set(activeFast());
        facade.fastingIsActive.set(true);
        facade.hasSnapshot.set(true);
        facade.isLoading.set(false);
        fixture.detectChanges();
        expect(host.querySelector('.dashboard__column')?.firstElementChild?.tagName).toBe('FD-DASHBOARD-FASTING-BLOCK');
    });
});

describe('Dashboard page actions and localization', () => {
    it('disables date editing, hides quick add and toggles cards instead of navigating in layout mode', async () => {
        const { fixture, layout, host, child, navigation, facade } = await setupAsync();
        layout.isEditingLayout.set(true);
        fixture.detectChanges();
        expect(host.querySelector('.dashboard--editing')).not.toBeNull();
        expect(host.querySelector('fd-dashboard-quick-add')).toBeNull();
        expect(child('fd-ui-date-picker-button').properties['disabled']).toBe(true);
        child('fd-dashboard-fasting-block').triggerEventHandler('blockClick');
        child('fd-dashboard-tdee-block').triggerEventHandler('blockClick', new MouseEvent('click'));
        expect(layout.toggleBlock.mock.calls).toEqual([['fasting'], ['tdee']]);
        expect(navigation.navigateToFastingAsync).not.toHaveBeenCalled();
        expect(facade.openTdeeDetailsAsync).not.toHaveBeenCalled();
        child('fd-dashboard-edit-hint').triggerEventHandler('save');
        expect(layout.save).toHaveBeenCalledOnce();
    });

    it('connects card actions to the correct facade and navigation operations', async () => {
        const { child, facade, navigation } = await setupAsync();
        child('fd-dashboard-hydration-block').triggerEventHandler('addClick', WATER_AMOUNT);
        child('fd-dashboard-hydration-block').triggerEventHandler('goalAction');
        child('fd-dashboard-meals-block').triggerEventHandler('open', { id: 'meal-42' });
        child('fd-dashboard-meals-block').triggerEventHandler('favoriteToggle', { id: 'meal-42' });
        child('fd-dashboard-meals-block').triggerEventHandler('add', 'Lunch');
        child('fd-dashboard-meals-block').triggerEventHandler('viewAll');
        child('fd-dashboard-tdee-block').triggerEventHandler('applyGoal', CALORIE_TARGET);
        child('fd-dashboard-fasting-block').triggerEventHandler('blockClick');
        expect(facade.addHydration).toHaveBeenCalledExactlyOnceWith(WATER_AMOUNT);
        expect(facade.openMealDetailsAsync).toHaveBeenCalledExactlyOnceWith('meal-42');
        expect(facade.toggleMealFavorite).toHaveBeenCalledExactlyOnceWith('meal-42');
        expect(facade.applyTdeeGoal).toHaveBeenCalledExactlyOnceWith(CALORIE_TARGET);
        expect(navigation.navigateToGoalsAsync).toHaveBeenCalledOnce();
        expect(navigation.navigateToMealAddAsync).toHaveBeenCalledExactlyOnceWith('Lunch');
        expect(navigation.navigateToMealListAsync).toHaveBeenCalledOnce();
        expect(navigation.navigateToFastingAsync).toHaveBeenCalledOnce();
    });

    it('updates the URL from the calendar and responds to browser date navigation', async () => {
        const { child, router, params, facade } = await setupAsync();
        child('fd-ui-date-picker-button').triggerEventHandler('valueChange', new Date('2026-09-03T00:00:00'));
        expect(router.navigate).toHaveBeenCalledWith(
            [],
            expect.objectContaining({ queryParams: { date: '2026-09-03' }, queryParamsHandling: 'merge' }),
        );
        router.navigate.mockClear();
        child('fd-ui-date-picker-button').triggerEventHandler('valueChange', null);
        expect(router.navigate).not.toHaveBeenCalled();
        params.next(convertToParamMap({ date: '2026-09-02' }));
        expect(facade.setSelectedDate).toHaveBeenLastCalledWith(new Date('2026-09-02T00:00:00'));
        params.next(convertToParamMap({ date: '2026-09-03' }));
        expect(facade.setSelectedDate).toHaveBeenLastCalledWith(new Date('2026-09-03T00:00:00'));
    });

    it('updates the calendar locale and dated title when the language changes', async () => {
        const { fixture, facade, child } = await setupAsync();
        const translate = TestBed.inject(TranslateService);
        facade.isTodaySelected.set(false);
        facade.selectedDate.set(new Date('2026-09-03T00:00:00'));
        translate.use('ru');
        fixture.detectChanges();
        expect(child('fd-ui-date-picker-button').properties['locale']).toBe('ru');
        expect(child('fd-ui-date-picker-button').properties['ariaLabel']).toContain('сентября');
        translate.use('en');
        fixture.detectChanges();
        expect(child('fd-ui-date-picker-button').properties['locale']).toBe('en');
        expect(child('fd-ui-date-picker-button').properties['ariaLabel']).toContain('September');
    });

    it('unregisters the same unsaved-changes handler when leaving the page', async () => {
        const { fixture, unsaved } = await setupAsync();
        const handler: unknown = unsaved.register.mock.calls[0]?.[0];
        expect(handler).toBeDefined();
        fixture.destroy();
        expect(unsaved.clear).toHaveBeenCalledExactlyOnceWith(handler);
    });
});

class DashboardTestState {
    public readonly facade = {
        selectedDate: signal(new Date()),
        isTodaySelected: signal(true),
        snapshot: signal({ dailyGoal: 2000, meals: { total: 0 } }),
        isLoading: signal(false),
        hasSnapshot: signal(true),
        meals: signal([]),
        weeklyConsumed: signal(0),
        weeklyCalories: signal([]),
        nutritionInsight: signal(null),
        hydration: signal({ totalMl: 500, goalMl: 2000 }),
        dailyAdvice: signal(null),
        isHydrationLoading: signal(false),
        isWeightTrendLoading: signal(false),
        isWaistTrendLoading: signal(false),
        isAdviceLoading: signal(false),
        cycle: signal(null),
        isCycleLoading: signal(false),
        tdeeInsight: signal(null),
        weightTrend: { weightTrendCurrent: signal(null), weightTrendChange: signal(null), weightTrendSeries: signal([]) },
        waistTrend: { waistTrendCurrent: signal(null), waistTrendChange: signal(null), waistTrendSeries: signal([]) },
        desiredWeightKg: signal(null),
        nutrientBars: signal([]),
        mealRingData: signal({ dailyGoal: 2000, dailyConsumed: 0, weeklyConsumed: 0, weeklyGoal: 14000, nutrientBars: [] }),
        mealPreviewEntries: signal([]),
        placeholderIcon: signal(''),
        placeholderLabel: signal(''),
        fastingIsActive: signal(false),
        currentFastingSession: signal<FastingSession | null>(null),
        favoriteLoadingIds: signal(new Set<string>()),
        initialize: vi.fn(),
        setSelectedDate: vi.fn(),
        openMealDetailsAsync: vi.fn(),
        toggleMealFavorite: vi.fn(),
        addHydration: vi.fn(),
        applyTdeeGoal: vi.fn(),
        openTdeeDetailsAsync: vi.fn().mockResolvedValue(undefined),
        reload: vi.fn(),
    };
    public readonly layout = {
        isEditingLayout: signal(false),
        hasLayoutChanges: signal(false),
        visibleBlocks: signal(['summary', 'meals', 'hydration', 'tdee']),
        hasAsideBlocks: signal(true),
        shouldRenderBlock: vi.fn(() => true),
        isBlockVisible: vi.fn(() => true),
        canToggleBlock: vi.fn(() => true),
        toggleBlock: vi.fn(),
        save: vi.fn(),
        discard: vi.fn(),
        openSettings: vi.fn(),
        updateViewportWidth: vi.fn(),
    };
    public readonly viewport = { isMobile: signal(false) };
    public readonly params = new BehaviorSubject(convertToParamMap({}));
    public readonly navigation = {
        navigateToFastingAsync: vi.fn(),
        navigateToGoalsAsync: vi.fn(),
        navigateToMealAddAsync: vi.fn(),
        navigateToMealListAsync: vi.fn(),
        navigateToWeightHistoryAsync: vi.fn(),
        navigateToCycleTrackingAsync: vi.fn(),
        navigateToProfileAsync: vi.fn(),
    };
    public readonly unsaved = { register: vi.fn<(handler: unknown) => void>(), clear: vi.fn() };
}

async function setupAsync(fasting = false, initiallyLoading = false): Promise<DashboardTestContext> {
    const { facade, layout, viewport, params, navigation, unsaved } = new DashboardTestState();
    facade.isLoading.set(initiallyLoading);
    facade.hasSnapshot.set(!initiallyLoading);
    facade.fastingIsActive.set(fasting);
    facade.currentFastingSession.set(fasting ? activeFast() : null);
    await TestBed.configureTestingModule({
        imports: [DashboardComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            { provide: ActivatedRoute, useValue: { queryParamMap: params } },
            { provide: NavigationService, useValue: navigation },
            { provide: UnsavedChangesService, useValue: unsaved },
            { provide: ViewportService, useValue: viewport },
            { provide: ThemeService, useValue: {} },
            { provide: FdUiDialogService, useValue: {} },
            { provide: FdTourService, useValue: { start: vi.fn() } },
            { provide: LocalizedTourDefinitionService, useValue: {} },
        ],
    })
        .overrideComponent(DashboardComponent, {
            set: {
                imports: [NgTemplateOutlet, TranslatePipe],
                schemas: [NO_ERRORS_SCHEMA],
                providers: [
                    { provide: DashboardFacade, useValue: facade },
                    { provide: DashboardLayoutService, useValue: layout },
                    {
                        provide: AiMealCreateFacade,
                        useValue: { isSaving: signal(false), clearToken: signal(0), createFromAiResult: vi.fn(() => of(null)) },
                    },
                ],
            },
        })
        .compileComponents();
    const fixture = TestBed.createComponent(DashboardComponent);
    const router = { navigate: vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true) };
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const child = (selector: string): DebugElement => {
        const element = fixture.debugElement.query(By.css(selector));
        expect(element, `Missing rendered child ${selector}`).not.toBeNull();
        return element;
    };
    return { fixture, facade, layout, viewport, params, navigation, unsaved, host, child, router };
}

function activeFast(): FastingSession {
    return {
        id: 'fast-1',
        startedAtUtc: new Date(Date.now() - ONE_HOUR_MS).toISOString(),
        endedAtUtc: null,
        initialPlannedDurationHours: 24,
        addedDurationHours: 0,
        plannedDurationHours: 24,
        protocol: 'Fast24',
        planType: 'Extended',
        occurrenceKind: 'FastingWindow',
        cyclicFastDays: null,
        cyclicEatDays: null,
        cyclicEatDayFastHours: null,
        cyclicEatDayEatingWindowHours: null,
        cyclicPhaseDayNumber: null,
        cyclicPhaseDayTotal: null,
        isCompleted: false,
        status: 'Active',
        notes: null,
        checkInAtUtc: null,
        hungerLevel: null,
        energyLevel: null,
        moodLevel: null,
        symptoms: [],
        checkInNotes: null,
        checkIns: [],
    };
}

type DashboardTestContext = {
    fixture: ComponentFixture<DashboardComponent>;
    facade: DashboardTestState['facade'];
    layout: DashboardTestState['layout'];
    viewport: DashboardTestState['viewport'];
    params: DashboardTestState['params'];
    navigation: DashboardTestState['navigation'];
    unsaved: DashboardTestState['unsaved'];
    host: HTMLElement;
    child: (selector: string) => DebugElement;
    router: { navigate: MockInstance<Router['navigate']> };
};
