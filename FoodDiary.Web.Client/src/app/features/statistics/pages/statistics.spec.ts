import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import type { ExportFormat } from '../../../shared/models/export.models';
import { StatisticsFacade } from '../lib/statistics.facade';
import type { StatisticsDashboardCardsView } from '../lib/statistics-dashboard-card.mapper';
import type { DateRange, NutritionChartTab, StatisticsRange } from '../lib/statistics-data-mapper';
import { StatisticsComponent } from './statistics';

const RANGE: DateRange = {
    start: new Date('2026-05-01T00:00:00Z'),
    end: new Date('2026-05-07T00:00:00Z'),
};
const REFRESHING_CARD_COUNT = 5;

type StatisticsFacadeMock = {
    changeNutritionTab: ReturnType<typeof vi.fn>;
    changeRange: ReturnType<typeof vi.fn>;
    currentRange: ReturnType<typeof signal<DateRange>>;
    dashboardCardsView: ReturnType<typeof signal<StatisticsDashboardCardsView>>;
    customRangeForm: FieldTree<{ range: { start: Date | null; end: Date | null } | null }>;
    exportDiary: ReturnType<typeof vi.fn>;
    exportingFormat: ReturnType<typeof signal<ExportFormat | null>>;
    hasBodyData: ReturnType<typeof signal<boolean>>;
    hasBodyLoadError: ReturnType<typeof signal<boolean>>;
    hasLoadError: ReturnType<typeof signal<boolean>>;
    hasStatisticsData: ReturnType<typeof signal<boolean>>;
    hasStatisticsResponse: ReturnType<typeof signal<boolean>>;
    initialize: ReturnType<typeof vi.fn>;
    isBodyLoading: ReturnType<typeof signal<boolean>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    reload: ReturnType<typeof vi.fn>;
    selectedNutritionTab: ReturnType<typeof signal<NutritionChartTab>>;
    selectedRange: ReturnType<typeof signal<StatisticsRange>>;
};

function createStatisticsFacadeMock(): StatisticsFacadeMock {
    const customRangeModel = signal<{ range: { start: Date | null; end: Date | null } | null }>({ range: null });
    const customRangeForm = form(customRangeModel);
    const dashboardCardsView: StatisticsDashboardCardsView = {
        overview: {
            daysWithinGoal: 0,
            trackedDays: 0,
            periodDays: 7,
            averageCalories: 0,
            calorieGoal: 0,
            calorieChangePercent: null,
            nutrients: [],
        },
        days: [],
        insights: [],
        balance: [],
        mealStructure: { totalCalories: 0, averageMealsPerDay: 0, dominantMeal: null, items: [] },
        stability: {
            stableCount: 0,
            totalCount: 0,
            averageDeviationPercent: null,
            longestLoggingStreak: 0,
            usesDailyIntervals: true,
            hasGoal: false,
            days: [],
        },
        body: {
            weight: { key: 'weight', current: null, change: null, goal: null, timeframeDays: 7, points: [] },
            waist: { key: 'waist', current: null, change: null, goal: null, timeframeDays: 7, points: [] },
        },
    };

    return {
        selectedRange: signal('week'),
        selectedNutritionTab: signal('calories'),
        customRangeForm,
        currentRange: signal(RANGE),
        dashboardCardsView: signal(dashboardCardsView),
        isLoading: signal(false),
        isBodyLoading: signal(false),
        hasLoadError: signal(false),
        hasBodyLoadError: signal(false),
        hasStatisticsData: signal(false),
        hasStatisticsResponse: signal(true),
        hasBodyData: signal(false),
        exportingFormat: signal(null),
        initialize: vi.fn(),
        changeRange: vi.fn(),
        changeNutritionTab: vi.fn(),
        reload: vi.fn(),
        exportDiary: vi.fn(),
    };
}

async function setupStatisticsAsync(): Promise<{
    component: StatisticsComponent;
    facade: StatisticsFacadeMock;
    fixture: ComponentFixture<StatisticsComponent>;
}> {
    const facadeRef: { current?: StatisticsFacadeMock } = {};

    TestBed.overrideComponent(StatisticsComponent, {
        set: {
            providers: [
                {
                    provide: StatisticsFacade,
                    useFactory: (): StatisticsFacadeMock => {
                        if (facadeRef.current === undefined) {
                            throw new Error('StatisticsFacade mock is not initialized.');
                        }

                        return facadeRef.current;
                    },
                },
            ],
        },
    });

    await TestBed.configureTestingModule({
        imports: [StatisticsComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
    }).compileComponents();

    facadeRef.current = TestBed.runInInjectionContext(() => createStatisticsFacadeMock());

    const fixture = TestBed.createComponent(StatisticsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, facade: facadeRef.current, fixture };
}

describe('StatisticsComponent', () => {
    it('initializes facade on creation', async () => {
        const { facade } = await setupStatisticsAsync();

        expect(facade.initialize).toHaveBeenCalledOnce();
    });

    it('delegates valid range and tab changes', async () => {
        const { component, facade } = await setupStatisticsAsync();

        component['changeRange']('month');
        component['changeNutritionTab']('macros');

        expect(facade.changeRange).toHaveBeenCalledWith('month');
        expect(facade.changeNutritionTab).toHaveBeenCalledWith('macros');
    });

    it('ignores invalid range and tab changes', async () => {
        const { component, facade } = await setupStatisticsAsync();

        component['changeRange']('invalid');
        component['changeNutritionTab']('invalid');

        expect(facade.changeRange).not.toHaveBeenCalled();
        expect(facade.changeNutritionTab).not.toHaveBeenCalled();
    });

    it('delegates reload and export actions', async () => {
        const { component, facade } = await setupStatisticsAsync();

        component['reload']();
        component['exportDiary']('csv');

        expect(facade.reload).toHaveBeenCalledOnce();
        expect(facade.exportDiary).toHaveBeenCalledWith('csv');
    });

    it('keeps cards mounted and overlays each dependent card while refreshing', async () => {
        const { facade, fixture } = await setupStatisticsAsync();

        facade.isLoading.set(true);
        facade.isBodyLoading.set(true);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('fd-statistics-overview-card')).not.toBeNull();
        expect(root.querySelector('fd-statistics-body-trend-card')).not.toBeNull();
        expect(root.querySelectorAll('.statistics__card-loading')).toHaveLength(REFRESHING_CARD_COUNT);
        expect(root.querySelector('.statistics__skeleton-grid')).toBeNull();
    });

    it('uses page skeletons only before the first statistics response', async () => {
        const { facade, fixture } = await setupStatisticsAsync();

        facade.hasStatisticsResponse.set(false);
        facade.isLoading.set(true);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('.statistics__skeleton-grid')).not.toBeNull();
        expect(root.querySelector('.statistics__new-cards')).toBeNull();
    });
});
