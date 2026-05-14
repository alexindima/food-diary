import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import { ExportService } from '../../meals/api/export.service';
import { WaistEntriesService } from '../../waist-history/api/waist-entries.service';
import { WeightEntriesService } from '../../weight-history/api/weight-entries.service';
import { StatisticsService } from '../api/statistics.service';
import { StatisticsFacade } from './statistics.facade';

const FIRST_TOTAL_CALORIES = 1800;
const USER_HEIGHT_CM = 180;
const RETRY_TOTAL_CALORIES = 2200;

let facade: StatisticsFacade;
let statisticsService: { getAggregatedStatistics: ReturnType<typeof vi.fn> };
let weightEntriesService: { getSummary: ReturnType<typeof vi.fn> };
let waistEntriesService: { getSummary: ReturnType<typeof vi.fn> };
let userService: { getInfo: ReturnType<typeof vi.fn> };
let exportService: { exportDiary: ReturnType<typeof vi.fn> };

beforeEach(() => {
    statisticsService = {
        getAggregatedStatistics: vi.fn().mockReturnValue(
            of([
                {
                    dateFrom: new Date('2026-04-01T00:00:00Z'),
                    dateTo: new Date('2026-04-01T23:59:59Z'),
                    totalCalories: FIRST_TOTAL_CALORIES,
                    averageProteins: 120,
                    averageFats: 70,
                    averageCarbs: 160,
                    averageFiber: 20,
                },
            ]),
        ),
    };
    weightEntriesService = {
        getSummary: vi
            .fn()
            .mockReturnValue(of([{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageWeight: 75.3 }])),
    };
    waistEntriesService = {
        getSummary: vi
            .fn()
            .mockReturnValue(of([{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageCircumference: 82.1 }])),
    };
    userService = {
        getInfo: vi.fn().mockReturnValue(of({ height: USER_HEIGHT_CM })),
    };
    exportService = {
        exportDiary: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
        providers: [
            StatisticsFacade,
            { provide: StatisticsService, useValue: statisticsService },
            { provide: WeightEntriesService, useValue: weightEntriesService },
            { provide: WaistEntriesService, useValue: waistEntriesService },
            { provide: UserService, useValue: userService },
            { provide: ExportService, useValue: exportService },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                    getCurrentLang: vi.fn(() => 'en'),
                    getFallbackLang: vi.fn(() => 'en'),
                },
            },
        ],
    });

    facade = TestBed.inject(StatisticsFacade);
});

describe('StatisticsFacade loading', () => {
    it('loads statistics, body summaries, and user profile on initialize', () => {
        facade.initialize();
        TestBed.tick();

        expect(statisticsService.getAggregatedStatistics).toHaveBeenCalled();
        expect(weightEntriesService.getSummary).toHaveBeenCalled();
        expect(waistEntriesService.getSummary).toHaveBeenCalled();
        expect(userService.getInfo).toHaveBeenCalled();
        expect(facade.chartStatisticsData()?.calories).toEqual([FIRST_TOTAL_CALORIES]);
        expect(facade.weightSummaryPoints()).toHaveLength(1);
        expect(facade.waistSummaryPoints()).toHaveLength(1);
        expect(facade.userHeightCm()).toBe(USER_HEIGHT_CM);
    });

    it('reloads aggregated data when the selected range changes', () => {
        facade.initialize();
        TestBed.tick();
        statisticsService.getAggregatedStatistics.mockClear();
        weightEntriesService.getSummary.mockClear();
        waistEntriesService.getSummary.mockClear();

        facade.changeRange('month');
        TestBed.tick();

        expect(statisticsService.getAggregatedStatistics).toHaveBeenCalledTimes(1);
        expect(weightEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(facade.selectedRange()).toBe('month');
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
        statisticsService.getAggregatedStatistics.mockReturnValueOnce(throwError(() => new Error('load failed')));

        facade.initialize();
        TestBed.tick();

        expect(facade.hasLoadError()).toBe(true);
        expect(facade.chartStatisticsData()).toBeNull();
    });

    it('marks body load error when body summaries request fails', () => {
        weightEntriesService.getSummary.mockReturnValueOnce(throwError(() => new Error('body failed')));

        facade.initialize();
        TestBed.tick();

        expect(facade.hasBodyLoadError()).toBe(true);
        expect(facade.weightSummaryPoints()).toEqual([]);
        expect(facade.waistSummaryPoints()).toEqual([]);
    });

    it('reload retries after a failed aggregated statistics request', () => {
        statisticsService.getAggregatedStatistics.mockReturnValueOnce(throwError(() => new Error('load failed'))).mockReturnValueOnce(
            of([
                {
                    dateFrom: new Date('2026-04-01T00:00:00Z'),
                    dateTo: new Date('2026-04-01T23:59:59Z'),
                    totalCalories: RETRY_TOTAL_CALORIES,
                    averageProteins: 130,
                    averageFats: 75,
                    averageCarbs: 190,
                    averageFiber: 24,
                },
            ]),
        );

        facade.initialize();
        TestBed.tick();

        expect(facade.hasLoadError()).toBe(true);

        facade.reload();
        TestBed.tick();

        expect(facade.hasLoadError()).toBe(false);
        expect(facade.chartStatisticsData()?.calories).toEqual([RETRY_TOTAL_CALORIES]);
    });
});
