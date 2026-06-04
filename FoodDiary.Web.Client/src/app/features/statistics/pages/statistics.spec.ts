import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { SummaryMetrics } from '../../../components/shared/statistics-summary/statistics-summary';
import type { ExportFormat } from '../../meals/models/export.models';
import { StatisticsFacade } from '../lib/statistics.facade';
import type { BodyChartTab, DateRange, NutritionChartTab, StatisticsRange } from '../lib/statistics-data-mapper';
import { StatisticsComponent } from './statistics';

const RANGE: DateRange = {
    start: new Date('2026-05-01T00:00:00Z'),
    end: new Date('2026-05-07T00:00:00Z'),
};

type StatisticsFacadeMock = {
    bodyChartPoints: ReturnType<typeof signal<unknown[]>>;
    caloriesTrendPoints: ReturnType<typeof signal<unknown[]>>;
    changeBodyTab: ReturnType<typeof vi.fn>;
    changeNutritionTab: ReturnType<typeof vi.fn>;
    changeRange: ReturnType<typeof vi.fn>;
    currentRange: ReturnType<typeof signal<DateRange>>;
    customRangeForm: FieldTree<{ range: { start: Date | null; end: Date | null } | null }>;
    exportDiary: ReturnType<typeof vi.fn>;
    exportingFormat: ReturnType<typeof signal<ExportFormat | null>>;
    hasBodyData: ReturnType<typeof signal<boolean>>;
    hasBodyLoadError: ReturnType<typeof signal<boolean>>;
    hasLoadError: ReturnType<typeof signal<boolean>>;
    hasStatisticsData: ReturnType<typeof signal<boolean>>;
    initialize: ReturnType<typeof vi.fn>;
    isBodyLoading: ReturnType<typeof signal<boolean>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    macroSparklinePoints: ReturnType<typeof signal<unknown[]>>;
    nutrientBarItems: ReturnType<typeof signal<unknown[]>>;
    nutrientPieSegments: ReturnType<typeof signal<unknown[]>>;
    nutrientTrendGroups: ReturnType<typeof signal<unknown[]>>;
    reload: ReturnType<typeof vi.fn>;
    selectedBodyTab: ReturnType<typeof signal<BodyChartTab>>;
    selectedNutritionTab: ReturnType<typeof signal<NutritionChartTab>>;
    selectedRange: ReturnType<typeof signal<StatisticsRange>>;
    summaryMetrics: ReturnType<typeof signal<SummaryMetrics | null>>;
    summarySparklinePoints: ReturnType<typeof signal<unknown[]>>;
};

function createStatisticsFacadeMock(): StatisticsFacadeMock {
    const customRangeModel = signal<{ range: { start: Date | null; end: Date | null } | null }>({ range: null });
    const customRangeForm = form(customRangeModel);

    return {
        selectedRange: signal('week'),
        selectedNutritionTab: signal('calories'),
        selectedBodyTab: signal('weight'),
        customRangeForm,
        currentRange: signal(RANGE),
        isLoading: signal(false),
        isBodyLoading: signal(false),
        hasLoadError: signal(false),
        hasBodyLoadError: signal(false),
        summaryMetrics: signal(null),
        summarySparklinePoints: signal([]),
        macroSparklinePoints: signal([]),
        hasStatisticsData: signal(false),
        caloriesTrendPoints: signal([]),
        nutrientTrendGroups: signal([]),
        nutrientPieSegments: signal([]),
        nutrientBarItems: signal([]),
        bodyChartPoints: signal([]),
        hasBodyData: signal(false),
        exportingFormat: signal(null),
        initialize: vi.fn(),
        changeRange: vi.fn(),
        changeNutritionTab: vi.fn(),
        changeBodyTab: vi.fn(),
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
        imports: [StatisticsComponent, TranslateModule.forRoot()],
        providers: [provideRouter([])],
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
        component['changeBodyTab']('bmi');

        expect(facade.changeRange).toHaveBeenCalledWith('month');
        expect(facade.changeNutritionTab).toHaveBeenCalledWith('macros');
        expect(facade.changeBodyTab).toHaveBeenCalledWith('bmi');
    });

    it('ignores invalid range and tab changes', async () => {
        const { component, facade } = await setupStatisticsAsync();

        component['changeRange']('invalid');
        component['changeNutritionTab']('invalid');
        component['changeBodyTab']('invalid');

        expect(facade.changeRange).not.toHaveBeenCalled();
        expect(facade.changeNutritionTab).not.toHaveBeenCalled();
        expect(facade.changeBodyTab).not.toHaveBeenCalled();
    });

    it('delegates reload and export actions', async () => {
        const { component, facade } = await setupStatisticsAsync();

        component['reload']();
        component['exportDiary']('csv');

        expect(facade.reload).toHaveBeenCalledOnce();
        expect(facade.exportDiary).toHaveBeenCalledWith('csv');
    });
});
