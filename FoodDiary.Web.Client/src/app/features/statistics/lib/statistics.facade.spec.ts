import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ExportService } from '../../../shared/api/export.service';
import { UserService } from '../../../shared/api/user.service';
import { StatisticsService } from '../api/statistics.service';
import type { AggregatedStatistics, StatisticsSummary } from '../models/statistics.data';
import { StatisticsFacade } from './statistics.facade';

const FIRST_TOTAL_CALORIES = 1800;
const USER_HEIGHT_CM = 180;
const RETRY_TOTAL_CALORIES = 2200;
const SECOND_WEIGHT_AVERAGE = 77.1;
const SECOND_WAIST_AVERAGE = 83.4;
const DEFAULT_WEIGHT_AVERAGE = 75.3;
const DEFAULT_WAIST_AVERAGE = 82.1;

let facade: StatisticsFacade;
let statisticsService: { getSummary: ReturnType<typeof vi.fn> };
let userService: { user: ReturnType<typeof signal<{ height: number } | null>> };
let exportService: { exportDiary: ReturnType<typeof vi.fn> };
let currentLanguage: string;
let languageChanges: Subject<unknown>;
let translateService: {
    instant: ReturnType<typeof vi.fn>;
    getCurrentLang: ReturnType<typeof vi.fn>;
    getFallbackLang: ReturnType<typeof vi.fn>;
    onLangChange: Subject<unknown>;
};

beforeEach(() => {
    statisticsService = {
        getSummary: vi.fn().mockReturnValue(of(createStatisticsSummary(FIRST_TOTAL_CALORIES))),
    };
    userService = {
        user: signal({ height: USER_HEIGHT_CM }),
    };
    exportService = {
        exportDiary: vi.fn().mockReturnValue(of(void 0)),
    };
    currentLanguage = 'en';
    languageChanges = new Subject<unknown>();
    translateService = {
        instant: vi.fn((key: string) => `${currentLanguage}:${key}`),
        getCurrentLang: vi.fn(() => currentLanguage),
        getFallbackLang: vi.fn(() => 'en'),
        onLangChange: languageChanges,
    };

    TestBed.configureTestingModule({
        providers: [
            StatisticsFacade,
            { provide: StatisticsService, useValue: statisticsService },
            { provide: UserService, useValue: userService },
            { provide: ExportService, useValue: exportService },
            { provide: TranslateService, useValue: translateService },
        ],
    });

    facade = TestBed.inject(StatisticsFacade);
});

describe('StatisticsFacade loading', () => {
    it('loads nutrition and body summaries in one request and reuses bootstrapped user profile', () => {
        facade.initialize();
        TestBed.tick();

        expect(statisticsService.getSummary).toHaveBeenCalledOnce();
        expect(facade.userProfile()).toEqual({ height: USER_HEIGHT_CM });
        expect(facade.chartStatisticsData()?.calories).toEqual([FIRST_TOTAL_CALORIES]);
        expect(facade.hasStatisticsResponse()).toBe(true);
        expect(facade.weightSummaryPoints()).toHaveLength(1);
        expect(facade.waistSummaryPoints()).toHaveLength(1);
    });

    it('reloads aggregated data when the selected range changes', () => {
        facade.initialize();
        TestBed.tick();
        statisticsService.getSummary.mockClear();

        facade.changeRange('month');
        TestBed.tick();

        expect(statisticsService.getSummary).toHaveBeenCalledTimes(1);
        expect(facade.selectedRange()).toBe('month');
    });

    it('keeps predefined ranges independent from custom range form changes', () => {
        facade.initialize();
        TestBed.tick();

        const currentRange = facade.currentRange();

        facade.customRangeModel.set({
            range: {
                start: new Date('2026-02-01T00:00:00Z'),
                end: new Date('2026-02-28T00:00:00Z'),
            },
        });
        TestBed.tick();

        expect(facade.currentRange()).toBe(currentRange);
    });
});

describe('StatisticsFacade stale requests', () => {
    it('ignores a stale combined summary response after range changes', () => {
        const requests = setupStaleRangeRequests();

        facade.initialize();
        TestBed.tick();
        facade.changeRange('month');
        TestBed.tick();

        completeLatestRangeRequests(requests);
        completeStaleRangeRequests(requests);

        expect(facade.chartStatisticsData()?.calories).toEqual([RETRY_TOTAL_CALORIES]);
        expect(facade.weightSummaryPoints()).toEqual([
            { startDate: '2026-04-02T00:00:00Z', endDate: '2026-04-02T23:59:59Z', averageWeightKg: SECOND_WEIGHT_AVERAGE },
        ]);
        expect(facade.waistSummaryPoints()).toEqual([
            { startDate: '2026-04-02T00:00:00Z', endDate: '2026-04-02T23:59:59Z', averageCircumferenceCm: SECOND_WAIST_AVERAGE },
        ]);
    });
});

describe('StatisticsFacade export', () => {
    it('exports current date range and tracks exporting format', () => {
        facade.initialize();
        TestBed.tick();

        facade.exportDiary('csv');
        TestBed.tick();

        expect(exportService.exportDiary).toHaveBeenCalledWith(
            expect.objectContaining({
                format: 'csv',
                locale: 'en',
            }),
        );
        expect(facade.exportingFormat()).toBeNull();
    });

    it('skips export while another export is in progress', () => {
        exportService.exportDiary.mockReturnValueOnce(new Subject<void>());

        facade.initialize();
        TestBed.tick();
        facade.exportDiary('pdf');
        facade.exportDiary('csv');

        expect(exportService.exportDiary).toHaveBeenCalledTimes(1);
        expect(facade.exportingFormat()).toBe('pdf');
    });
});

describe('StatisticsFacade errors', () => {
    it('marks load error when aggregated statistics request fails', () => {
        statisticsService.getSummary.mockReturnValueOnce(throwError(() => new Error('load failed')));

        facade.initialize();
        TestBed.tick();

        expect(facade.hasLoadError()).toBe(true);
        expect(facade.chartStatisticsData()).toBeNull();
    });

    it('reload retries after a failed aggregated statistics request', () => {
        statisticsService.getSummary
            .mockReturnValueOnce(throwError(() => new Error('load failed')))
            .mockReturnValueOnce(of(createStatisticsSummary(RETRY_TOTAL_CALORIES)));

        facade.initialize();
        TestBed.tick();

        expect(facade.hasLoadError()).toBe(true);

        facade.reload();
        TestBed.tick();

        expect(facade.hasLoadError()).toBe(false);
        expect(facade.chartStatisticsData()?.calories).toEqual([RETRY_TOTAL_CALORIES]);
    });
});

function createStatisticsResponse(totalCalories: number): AggregatedStatistics[] {
    return [
        {
            dateFrom: new Date('2026-04-01T00:00:00Z'),
            dateTo: new Date('2026-04-01T23:59:59Z'),
            totalCalories,
            averageProteins: 120,
            averageFats: 70,
            averageCarbs: 160,
            averageFiber: 20,
            totalProteins: 120,
            totalFats: 70,
            totalCarbs: 160,
            totalFiber: 20,
        },
    ];
}

function createStatisticsSummary(
    totalCalories: number,
    averageWeightKg = DEFAULT_WEIGHT_AVERAGE,
    averageCircumferenceCm = DEFAULT_WAIST_AVERAGE,
): StatisticsSummary {
    return {
        nutrition: createStatisticsResponse(totalCalories),
        weight: [{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageWeightKg }],
        waist: [{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageCircumferenceCm }],
    };
}

type StaleRangeRequests = {
    firstSummary$: Subject<StatisticsSummary>;
    secondSummary$: Subject<StatisticsSummary>;
};

function setupStaleRangeRequests(): StaleRangeRequests {
    const requests = {
        firstSummary$: new Subject<StatisticsSummary>(),
        secondSummary$: new Subject<StatisticsSummary>(),
    };
    statisticsService.getSummary.mockReturnValueOnce(requests.firstSummary$).mockReturnValueOnce(requests.secondSummary$);

    return requests;
}

function completeLatestRangeRequests(requests: StaleRangeRequests): void {
    const summary = createStatisticsSummary(RETRY_TOTAL_CALORIES, SECOND_WEIGHT_AVERAGE, SECOND_WAIST_AVERAGE);
    summary.weight[0] = { ...summary.weight[0], startDate: '2026-04-02T00:00:00Z', endDate: '2026-04-02T23:59:59Z' };
    summary.waist[0] = { ...summary.waist[0], startDate: '2026-04-02T00:00:00Z', endDate: '2026-04-02T23:59:59Z' };
    requests.secondSummary$.next(summary);
    requests.secondSummary$.complete();
    TestBed.tick();
}

function completeStaleRangeRequests(requests: StaleRangeRequests): void {
    requests.firstSummary$.next(createStatisticsSummary(FIRST_TOTAL_CALORIES));
    requests.firstSummary$.complete();
    TestBed.tick();
}
