import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { firstValueFrom, of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { ExportService } from '../../../shared/api/export.service';
import { UserService } from '../../../shared/api/user.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../shared/development/development-tools';
import { MeasurementSystemService } from '../../../shared/measurements/measurement-system.service';
import type { ExportFormat } from '../../../shared/models/export.models';
import { calendarDate } from '../../../shared/models/semantics/date-value';
import { StatisticsService } from '../api/statistics.service';
import { StatisticsFacade } from '../lib/statistics.facade';
import type { StatisticsDashboardCardsView } from '../lib/statistics-dashboard-card.mapper';
import type { DateRange, NutritionChartTab, StatisticsRange } from '../lib/statistics-data-mapper';
import type { StatisticsSummary } from '../models/statistics.data';
import { StatisticsComponent } from './statistics';

const RANGE: DateRange = {
    start: new Date('2026-05-01T00:00:00Z'),
    end: new Date('2026-05-07T00:00:00Z'),
};
const REFRESHING_CARD_COUNT = 5;
const REPORT_START = '2026-10-31';
const REPORT_END = '2026-11-01';
const EDITED_REPORT_END = '2026-11-04';
const CURRENT_REPORT_CALORIES = 2200;
const OLD_REPORT_CALORIES = 1900;
const WAIST_HISTORY_URL = '/waist-history';
const WEIGHT_HISTORY_URL = '/weight-history';
const OCTOBER_WAIST = 94;
const NOVEMBER_WAIST = 92;

@Component({ template: '<h1>Waist history</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class StatisticsHistoryRouteComponent {}

@Component({
    selector: 'fd-statistics-test-weight-history',
    template: '<h1>Weight history</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class StatisticsWeightHistoryRouteComponent {}

type StatisticsFacadeMock = {
    changeNutritionTab: ReturnType<typeof vi.fn>;
    changeRange: ReturnType<typeof vi.fn>;
    currentRange: ReturnType<typeof signal<DateRange>>;
    dashboardCardsView: ReturnType<typeof signal<StatisticsDashboardCardsView>>;
    customRangeForm: FieldTree<{ range: { start: Date | null; end: Date | null } | null }>;
    customRangeModel: ReturnType<typeof signal<{ range: { start: Date | null; end: Date | null } | null }>>;
    exportDiary: ReturnType<typeof vi.fn>;
    exportError: ReturnType<typeof signal<boolean>>;
    exportingFormat: ReturnType<typeof signal<ExportFormat | null>>;
    hasBodyData: ReturnType<typeof signal<boolean>>;
    hasBodyLoadError: ReturnType<typeof signal<boolean>>;
    hasLoadError: ReturnType<typeof signal<boolean>>;
    hasReportError: ReturnType<typeof computed<boolean>>;
    hasStatisticsData: ReturnType<typeof signal<boolean>>;
    hasStatisticsResponse: ReturnType<typeof signal<boolean>>;
    initialize: ReturnType<typeof vi.fn>;
    isBodyLoading: ReturnType<typeof signal<boolean>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    periodValidationError: ReturnType<typeof signal<string | null>>;
    reload: ReturnType<typeof vi.fn>;
    selectedNutritionTab: ReturnType<typeof signal<NutritionChartTab>>;
    selectedRange: ReturnType<typeof signal<StatisticsRange>>;
};

function createStatisticsFacadeMock(): StatisticsFacadeMock {
    const customRangeModel = signal<{ range: { start: Date | null; end: Date | null } | null }>({ range: null });
    const customRangeForm = form(customRangeModel);
    const hasLoadError = signal(false);
    const periodValidationError = signal<string | null>(null);
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
            weight: { key: 'weight', current: null, change: null, goal: null, timeframeDays: 7, measurementCount: 0, points: [] },
            waist: { key: 'waist', current: null, change: null, goal: null, timeframeDays: 7, measurementCount: 0, points: [] },
        },
    };

    return {
        selectedRange: signal('week'),
        selectedNutritionTab: signal('calories'),
        customRangeForm,
        customRangeModel,
        currentRange: signal(RANGE),
        dashboardCardsView: signal(dashboardCardsView),
        isLoading: signal(false),
        periodValidationError,
        isBodyLoading: signal(false),
        hasLoadError,
        hasReportError: computed(() => hasLoadError() || periodValidationError() !== null),
        hasBodyLoadError: signal(false),
        hasStatisticsData: signal(false),
        hasStatisticsResponse: signal(true),
        hasBodyData: signal(false),
        exportingFormat: signal(null),
        exportError: signal(false),
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

    it('shows an accessible export alert only while the facade reports a failed download', async () => {
        const { facade, fixture } = await setupStatisticsAsync();
        const alert = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('fd-ui-inline-alert[role="alert"]');
        expect(alert?.hidden).toBe(true);
        facade.exportError.set(true);
        fixture.detectChanges();
        expect(alert?.hidden).toBe(false);
        facade.exportError.set(false);
        fixture.detectChanges();
        expect(alert?.hidden).toBe(true);
    });

    it('keeps cards mounted and overlays each dependent card while refreshing', async () => {
        const { facade, fixture } = await setupStatisticsAsync();

        facade.isLoading.set(true);
        facade.isBodyLoading.set(true);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('fd-statistics-overview-card')).not.toBeNull();
        expect(root.querySelectorAll('fd-statistics-overview-card')).toHaveLength(1);
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

describe('Statistics report retry and download', () => {
    it('retries the selected period and exports the same local boundaries from the native menu', async () => {
        const api = createReportApi();
        const failedReport = new Subject<StatisticsSummary>();
        const downloaded = new Subject<void>();
        api.getSummary.mockReturnValueOnce(failedReport).mockReturnValueOnce(of(reportSummary(CURRENT_REPORT_CALORIES)));
        api.exportDiary.mockReturnValueOnce(downloaded);
        const url = `/statistics?range=custom&dateFrom=${REPORT_START}&dateTo=${REPORT_END}`;
        const { harness, host } = await openReportAsync(url, api);

        expect(host.querySelector('.statistics__skeleton-grid')).not.toBeNull();
        failedReport.error(new Error('Summary unavailable'));
        harness.detectChanges();
        expect(host.querySelector('fd-statistics-overview-card')).toBeNull();
        reportButton(host, 'fd-error-state button').click();
        harness.detectChanges();

        expect(api.getSummary).toHaveBeenCalledTimes(2);
        expect(api.getSummary.mock.calls[1][0]).toMatchObject({ bodyDateFrom: REPORT_START, bodyDateTo: REPORT_END });
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(host.querySelector('fd-statistics-overview-card')?.textContent).toContain('2,200');

        const exportMenu = reportButton(host, 'button[aria-label="STATISTICS.EXPORT_MENU_LABEL"]');
        exportMenu.click();
        const selectPdf = await vi.waitFor(() => {
            harness.detectChanges();
            return labelButton(document.body, 'STATISTICS.EXPORT_PDF', '[role="menuitem"]');
        });
        selectPdf.click();
        harness.detectChanges();

        expect(api.exportDiary).toHaveBeenCalledOnce();
        expect(api.exportDiary.mock.calls[0][0]).toMatchObject({
            format: 'pdf',
            dateFrom: new Date(`${REPORT_START}T00:00:00.000`).toISOString(),
            dateTo: new Date(`${REPORT_END}T23:59:59.999`).toISOString(),
        });
        expect(exportMenu.disabled).toBe(true);
        exportMenu.click();
        expect(api.exportDiary).toHaveBeenCalledOnce();

        downloaded.error(new Error('PDF unavailable'));
        harness.detectChanges();
        expect(exportMenu.disabled).toBe(false);
        expect(host.querySelector('[role="alert"]:not([hidden])')?.textContent).toContain('STATISTICS.EXPORT_FAILED');
    });
});

describe('Statistics report period controls', () => {
    it('disables report and export for an oversized period until a supported native filter is picked', async () => {
        const api = createReportApi();
        const { harness, host, router } = await openReportAsync('/statistics?range=custom&dateFrom=2024-01-01&dateTo=2025-01-01', api);
        const exportMenu = reportButton(host, 'button[aria-label="STATISTICS.EXPORT_MENU_LABEL"]');

        expect(host.querySelector('[role="alert"]:not([hidden])')?.textContent).toContain('STATISTICS.PERIOD_TOO_LONG');
        expect(exportMenu.disabled).toBe(true);
        exportMenu.click();
        expect(api.exportDiary).not.toHaveBeenCalled();
        expect(api.getSummary).not.toHaveBeenCalled();

        labelButton(host, 'STATISTICS.RANGES.MONTH', 'fd-period-filter button').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe('/statistics?range=month');
            expect(api.getSummary).toHaveBeenCalledOnce();
        });
        expect(exportMenu.disabled).toBe(false);
        expect(host.querySelector('[role="alert"]:not([hidden])')).toBeNull();
    });

    it('renders only the newly selected report even if an older response arrives afterward', async () => {
        const api = createReportApi();
        const previous = new Subject<StatisticsSummary>();
        const current = new Subject<StatisticsSummary>();
        api.getSummary.mockReturnValueOnce(previous).mockReturnValueOnce(current);
        const { harness, host, router } = await openReportAsync('/statistics', api);
        expect(api.getSummary).toHaveBeenCalledOnce();

        labelButton(host, 'STATISTICS.RANGES.MONTH', 'fd-period-filter button').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe('/statistics?range=month');
            expect(api.getSummary).toHaveBeenCalledTimes(2);
        });
        current.next(reportSummary(CURRENT_REPORT_CALORIES));
        current.complete();
        harness.detectChanges();
        expect(host.querySelector('fd-statistics-overview-card')?.textContent).toContain('2,200');

        previous.next(reportSummary(OLD_REPORT_CALORIES));
        previous.complete();
        harness.detectChanges();

        expect(host.querySelector('fd-statistics-overview-card')?.textContent).toContain('2,200');
        expect(host.querySelector('fd-statistics-overview-card')?.textContent).not.toContain('1,900');
    });
});

describe('Statistics native date and route synchronization', () => {
    it('keeps the last period selection when an earlier navigation has not landed', async () => {
        const api = createReportApi();
        const original = '/statistics?campaign=qa';
        const { harness, host, router } = await openReportAsync(original, api);
        const month = labelButton(host, 'STATISTICS.RANGES.MONTH', 'fd-period-filter button');
        const week = labelButton(host, 'STATISTICS.RANGES.WEEK', 'fd-period-filter button');

        month.click();
        harness.detectChanges();
        expect(month.getAttribute('aria-checked')).toBe('true');
        expect(router.url).toBe(original);
        week.click();
        harness.detectChanges();
        expect(week.getAttribute('aria-checked')).toBe('true');

        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(router.url).toBe(original);
        expect(week.getAttribute('aria-checked')).toBe('true');
        expect(month.getAttribute('aria-checked')).toBe('false');
    });

    it('shares the date picked in the native calendar across the route, summary and CSV export', async () => {
        const api = createReportApi();
        const { harness, host, router } = await openReportAsync(
            `/statistics?range=custom&dateFrom=${REPORT_START}&dateTo=${REPORT_END}&campaign=qa`,
            api,
        );
        const trigger = reportButton(host, 'fd-ui-date-range-input fd-ui-date-input:nth-of-type(2) button[aria-haspopup="dialog"]');
        trigger.click();
        const day = await vi.waitFor(() => {
            harness.detectChanges();
            return reportButton(document.body, `[role="dialog"] [data-date="${EDITED_REPORT_END}"]`);
        });
        expect(day.disabled).toBe(false);
        expect(api.getSummary).toHaveBeenCalledTimes(1);
        day.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.parseUrl(router.url).queryParams).toEqual({
                range: 'custom',
                dateFrom: REPORT_START,
                dateTo: EDITED_REPORT_END,
                campaign: 'qa',
            });
        });
        expect(api.getSummary).toHaveBeenCalledTimes(2);
        expect(api.getSummary.mock.lastCall?.[0]).toMatchObject({ bodyDateFrom: REPORT_START, bodyDateTo: EDITED_REPORT_END });
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        reportButton(host, 'button[aria-label="STATISTICS.EXPORT_MENU_LABEL"]').click();
        const csv = await vi.waitFor(() => {
            harness.detectChanges();
            return labelButton(document.body, 'STATISTICS.EXPORT_CSV', '[role="menuitem"]');
        });
        csv.click();

        expect(api.exportDiary).toHaveBeenCalledTimes(1);
        expect(api.exportDiary.mock.lastCall?.[0]).toMatchObject({
            format: 'csv',
            dateFrom: new Date(`${REPORT_START}T00:00:00.000`).toISOString(),
            dateTo: new Date(`${EDITED_REPORT_END}T23:59:59.999`).toISOString(),
        });
    });
});

describe('Statistics live report capability and controls', () => {
    it('updates the native tabs and distinguishes the selected period from the report still loading', async () => {
        const api = createReportApi();
        const first = new Subject<StatisticsSummary>();
        const latest = new Subject<StatisticsSummary>();
        api.getSummary.mockReturnValueOnce(first).mockReturnValueOnce(latest);
        const { harness, host, router, developmentScreens } = await openReportAsync('/statistics?campaign=qa', api);
        const screen = developmentScreens.get('statistics');
        if (screen?.setFilters === undefined) {
            throw new Error('Expected the statistics development filters');
        }
        expect(screen.read()).toMatchObject({ loading: true, appliedPeriod: null, available: false, aggregates: null });
        expect(host.querySelector('.statistics__skeleton-grid')).not.toBeNull();
        expect(() => screen.setFilters?.({ range: 'month', dateFrom: REPORT_START, dateTo: REPORT_END })).toThrow(
            'ordered custom calendar range',
        );
        expect(() => screen.setFilters?.({ nutritionTab: 'unknown' })).toThrow('Unsupported statistics range or nutrition tab');
        expect(api.getSummary).toHaveBeenCalledTimes(1);
        expect(router.url).toBe('/statistics?campaign=qa');

        screen.setFilters({ dateFrom: REPORT_START, dateTo: REPORT_END, nutritionTab: 'macros' });
        const period = { dateFrom: REPORT_START, dateTo: REPORT_END };
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.parseUrl(router.url).queryParams).toEqual({ range: 'custom', nutritionTab: 'macros', campaign: 'qa', ...period });
        });
        expect(latest.observed).toBe(true);
        first.next(reportSummary(OLD_REPORT_CALORIES));
        first.complete();
        harness.detectChanges();
        expect(screen.read()).toMatchObject({ period, appliedPeriod: null, loading: true, available: false, aggregates: null });

        latest.next(reportSummary(CURRENT_REPORT_CALORIES));
        latest.complete();
        harness.detectChanges();
        expect(screen.read()).toMatchObject({
            period,
            appliedPeriod: period,
            available: true,
            aggregates: { totalCalories: CURRENT_REPORT_CALORIES },
        });
        expect(
            labelButton(host, 'STATISTICS.NUTRITION_TABS.MACROS', 'fd-statistics-nutrition-trend-card button').getAttribute('aria-checked'),
        ).toBe('true');
        screen.setFilters({ nutritionTab: 'distribution' });
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.parseUrl(router.url).queryParams['nutritionTab']).toBe('distribution');
        });
        expect(api.getSummary).toHaveBeenCalledTimes(2);
        expect(screen.read()).toMatchObject({ period, appliedPeriod: period, available: true, nutritionTab: 'distribution' });
    });

    it('opens the report tour from the accessible toolbar action', async () => {
        const { host } = await openReportAsync('/statistics', createReportApi());
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        expect(start).not.toHaveBeenCalled();

        reportButton(host, '[data-tour-id="statistics-tour-help"] button').click();

        expect(start).toHaveBeenCalledExactlyOnceWith(expect.any(Object), { force: true });
    });
});

describe('Statistics report with weight data only', () => {
    it('shows weight without nutrition or waist data and opens the correct history', async () => {
        const api = createReportApi();
        const firstWeight = 82;
        const latestWeight = 81;
        api.getSummary.mockReturnValue(
            of({
                nutrition: [],
                weight: [
                    { startDate: calendarDate(REPORT_START), endDate: calendarDate(REPORT_START), averageWeightKg: firstWeight },
                    { startDate: calendarDate(REPORT_END), endDate: calendarDate(REPORT_END), averageWeightKg: latestWeight },
                ],
                waist: [],
                weightMeasurements: { first: firstWeight, last: latestWeight, count: 2 },
                waistMeasurements: { first: null, last: null, count: 0 },
            }),
        );
        const { harness, host, router, developmentScreens } = await openReportAsync(
            `/statistics?range=custom&dateFrom=${REPORT_START}&dateTo=${REPORT_END}`,
            api,
        );
        TestBed.inject(MeasurementSystemService).setSystem('metric');
        harness.detectChanges();
        expect(host.querySelector('fd-ui-empty-state')?.textContent).toContain('STATISTICS.NO_DATA');
        expect(host.querySelector('.statistics__nutrition-group:not([hidden])')).toBeNull();
        const bodyCard = host.querySelector<HTMLElement>('fd-statistics-body-trend-card');
        if (bodyCard === null) {
            throw new Error('Expected the weight chart');
        }
        expect(bodyCard.parentElement?.hidden).toBe(false);
        expect(bodyCard.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(bodyCard.textContent).toContain(`${latestWeight} GENERAL.UNITS.KG`);
        expect(developmentScreens.has('statistics')).toBe(true);

        labelButton(bodyCard, 'STATISTICS.DASHBOARD.BODY.OPEN_WEIGHT_HISTORY', 'button').click();
        await harness.fixture.whenStable();

        expect(router.url).toBe(WEIGHT_HISTORY_URL);
        expect(harness.routeNativeElement?.textContent).toContain('Weight history');
        expect(developmentScreens.has('statistics')).toBe(false);
        expect(api.getSummary).toHaveBeenCalledTimes(1);
    });
});

describe('Statistics report with body measurements only', () => {
    it('shows a standalone waist trend, relabels it without reloading, and opens waist history', async () => {
        const api = createReportApi();
        api.getSummary.mockReturnValue(
            of({
                nutrition: [],
                weight: [],
                waist: [
                    {
                        startDate: calendarDate('2026-10-01'),
                        endDate: calendarDate(REPORT_START),
                        averageCircumferenceCm: OCTOBER_WAIST,
                    },
                    {
                        startDate: calendarDate(`${REPORT_END}T00:00:00.000`),
                        endDate: calendarDate('2026-11-30'),
                        averageCircumferenceCm: NOVEMBER_WAIST,
                    },
                ],
                weightMeasurements: { first: null, last: null, count: 0 },
                waistMeasurements: { first: OCTOBER_WAIST, last: NOVEMBER_WAIST, count: 2 },
            }),
        );
        const { harness, host, router } = await openReportAsync('/statistics?range=year', api);
        TestBed.inject(MeasurementSystemService).setSystem('metric');
        harness.detectChanges();
        expect(host.querySelector('fd-ui-empty-state')?.textContent).toContain('STATISTICS.NO_DATA');
        expect(host.querySelector('.statistics__nutrition-group:not([hidden])')).toBeNull();
        const bodyCard = host.querySelector<HTMLElement>('fd-statistics-body-trend-card');
        if (bodyCard === null) {
            throw new Error('Expected the report to show the waist measurements');
        }
        expect(bodyCard.parentElement?.hidden).toBe(false);
        expect(bodyCard.querySelector('fd-ui-line-chart')).toBeNull();
        expect(bodyCard.textContent).toContain('STATISTICS.DASHBOARD.BODY.EMPTY_WEIGHT');

        labelButton(bodyCard, 'STATISTICS.DASHBOARD.BODY.WAIST', 'button').click();
        harness.detectChanges();
        const axis = bodyCard.querySelector('fd-ui-line-chart-x-axis');
        expect(axis?.textContent).toContain('Oct');
        expect(axis?.textContent).toContain('Nov');
        expect(bodyCard.textContent).toContain(`${NOVEMBER_WAIST} GENERAL.UNITS.CM`);
        expect(bodyCard.querySelector('.fd-ui-line-chart__reference-line')).toBeNull();
        await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
        harness.detectChanges();
        expect(axis?.textContent).toMatch(/Окт/u);
        expect(axis?.textContent).toMatch(/Ноя/u);
        expect(api.getSummary).toHaveBeenCalledOnce();

        labelButton(bodyCard, 'STATISTICS.DASHBOARD.BODY.OPEN_WAIST_HISTORY', 'button').click();
        await harness.fixture.whenStable();
        expect(router.url).toBe(WAIST_HISTORY_URL);
        expect(harness.routeNativeElement?.textContent).toContain('Waist history');
    });
});

describe('Statistics report chart with calorie-only days', () => {
    it('keeps a manually logged day visible and avoids creating a nutrient distribution or goal', async () => {
        const api = createReportApi();
        const summary = reportSummary(CURRENT_REPORT_CALORIES);
        summary.nutrition = summary.nutrition.map(day => ({
            ...day,
            averageProteins: 0,
            averageFats: 0,
            averageCarbs: 0,
            averageFiber: 0,
            totalProteins: 0,
            totalFats: 0,
            totalCarbs: 0,
            totalFiber: 0,
        }));
        api.getSummary.mockReturnValue(of(summary));
        const { harness, host } = await openReportAsync('/statistics', api);
        const trend = host.querySelector<HTMLElement>('fd-statistics-nutrition-trend-card');
        if (trend === null) {
            throw new Error('Expected a trend for a recorded calorie day');
        }
        expect(trend.querySelector('.fd-ui-bar-chart__categorical-bar')?.getAttribute('aria-label')).toContain(
            CURRENT_REPORT_CALORIES.toString(),
        );
        expect(trend.querySelector('.fd-ui-bar-chart__reference-line')).toBeNull();

        const distribution = labelButton(trend, 'STATISTICS.NUTRITION_TABS.DISTRIBUTION', 'button');
        distribution.click();
        harness.detectChanges();
        expect(distribution.getAttribute('aria-checked')).toBe('true');
        const segments = [...trend.querySelectorAll<HTMLElement>('.fd-ui-bar-chart__categorical-segment')];
        expect(segments.length).toBeGreaterThan(0);
        expect(segments.every(segment => segment.style.height === '0%')).toBe(true);
        expect(trend.querySelector('.fd-ui-bar-chart__axis-unit')?.textContent).toContain('STATISTICS.DASHBOARD.TREND.PERCENT_UNIT');

        labelButton(trend, 'STATISTICS.DASHBOARD.TREND.CHART_LINE', 'button').click();
        harness.detectChanges();
        expect(trend.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(trend.innerHTML).not.toMatch(/NaN|Infinity/u);
        expect(api.getSummary).toHaveBeenCalledOnce();
    });
});

describe('Statistics report route ownership', () => {
    it('cancels the report when leaving and loads a fresh report on return', async () => {
        const api = createReportApi();
        const pending = new Subject<StatisticsSummary>();
        api.getSummary.mockReturnValueOnce(pending);
        const { harness, host } = await openReportAsync('/statistics', api);
        expect(host.querySelector('.statistics__skeleton-grid')).not.toBeNull();
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(WAIST_HISTORY_URL, StatisticsHistoryRouteComponent);
        expect(pending.observed).toBe(false);
        pending.next(reportSummary(OLD_REPORT_CALORIES));
        pending.complete();
        expect(harness.routeNativeElement?.textContent).toContain('Waist history');

        await harness.navigateByUrl('/statistics', StatisticsComponent);
        expect(harness.routeNativeElement?.querySelector('fd-statistics-overview-card')?.textContent).toContain('2,200');
        expect(api.getSummary).toHaveBeenCalledTimes(2);
    });
});

type StatisticsReportApi = {
    getSummary: ReturnType<typeof vi.fn<StatisticsService['getSummary']>>;
    exportDiary: ReturnType<typeof vi.fn<ExportService['exportDiary']>>;
};

function createReportApi(): StatisticsReportApi {
    return {
        getSummary: vi.fn<StatisticsService['getSummary']>().mockReturnValue(of(reportSummary(CURRENT_REPORT_CALORIES))),
        exportDiary: vi.fn<ExportService['exportDiary']>().mockReturnValue(of(void 0)),
    };
}

async function openReportAsync(
    url: string,
    api: StatisticsReportApi,
): Promise<{
    harness: RouterTestingHarness;
    host: HTMLElement;
    router: Router;
    developmentScreens: Map<string, DevelopmentScreen>;
}> {
    const developmentScreens = new Map<string, DevelopmentScreen>();
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            provideRouter([
                { path: 'statistics', component: StatisticsComponent },
                { path: 'waist-history', component: StatisticsHistoryRouteComponent },
                { path: 'weight-history', component: StatisticsWeightHistoryRouteComponent },
            ]),
            {
                provide: DEVELOPMENT_SCREENS,
                useValue: {
                    attach: (screen: DevelopmentScreen): (() => void) => {
                        developmentScreens.set(screen.kind, screen);
                        return (): void => {
                            developmentScreens.delete(screen.kind);
                        };
                    },
                },
            },
            { provide: StatisticsService, useValue: { getSummary: api.getSummary } },
            { provide: ExportService, useValue: { exportDiary: api.exportDiary } },
            { provide: UserService, useValue: { user: signal(null) } },
        ],
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, StatisticsComponent);
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Statistics route did not mount');
    }
    return { harness, host, router: TestBed.inject(Router), developmentScreens };
}

function reportSummary(totalCalories: number): StatisticsSummary {
    return {
        nutrition: [
            {
                dateFrom: new Date(`${REPORT_START}T00:00:00.000`),
                dateTo: new Date(`${REPORT_START}T23:59:59.999`),
                totalCalories,
                averageProteins: 100,
                averageFats: 70,
                averageCarbs: 250,
                averageFiber: 25,
                totalProteins: 100,
                totalFats: 70,
                totalCarbs: 250,
                totalFiber: 25,
                mealCount: 1,
                trackedDayCount: 1,
            },
        ],
        weight: [],
        waist: [],
        weightMeasurements: { first: null, last: null, count: 0 },
        waistMeasurements: { first: null, last: null, count: 0 },
    };
}

function reportButton(host: HTMLElement, selector: string): HTMLButtonElement {
    const button = host.querySelector<HTMLButtonElement>(selector);
    if (button === null) {
        throw new Error(`Missing report control ${selector}`);
    }
    return button;
}

function labelButton(host: HTMLElement, label: string, selector: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll<HTMLButtonElement>(selector)).find(candidate =>
        candidate.textContent.trim().endsWith(label),
    );
    if (button === undefined) {
        throw new Error(`Missing report action ${label}`);
    }
    return button;
}
