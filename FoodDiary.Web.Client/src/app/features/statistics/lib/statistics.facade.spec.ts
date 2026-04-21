import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { StatisticsFacade } from './statistics.facade';
import { StatisticsService } from '../api/statistics.service';
import { WeightEntriesService } from '../../weight-history/api/weight-entries.service';
import { WaistEntriesService } from '../../waist-history/api/waist-entries.service';
import { UserService } from '../../../shared/api/user.service';

describe('StatisticsFacade', () => {
    let facade: StatisticsFacade;
    let statisticsService: { getAggregatedStatistics: ReturnType<typeof vi.fn> };
    let weightEntriesService: { getSummary: ReturnType<typeof vi.fn> };
    let waistEntriesService: { getSummary: ReturnType<typeof vi.fn> };
    let userService: { getInfo: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        statisticsService = {
            getAggregatedStatistics: vi.fn().mockReturnValue(
                of([
                    {
                        dateFrom: new Date('2026-04-01T00:00:00Z'),
                        dateTo: new Date('2026-04-01T23:59:59Z'),
                        totalCalories: 1800,
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
                .mockReturnValue(of([{ dateFrom: '2026-04-01T00:00:00Z', dateTo: '2026-04-01T23:59:59Z', averageWeight: 75.3 }])),
        };
        waistEntriesService = {
            getSummary: vi
                .fn()
                .mockReturnValue(of([{ dateFrom: '2026-04-01T00:00:00Z', dateTo: '2026-04-01T23:59:59Z', averageCircumference: 82.1 }])),
        };
        userService = {
            getInfo: vi.fn().mockReturnValue(of({ height: 180 })),
        };

        TestBed.configureTestingModule({
            providers: [
                StatisticsFacade,
                { provide: StatisticsService, useValue: statisticsService },
                { provide: WeightEntriesService, useValue: weightEntriesService },
                { provide: WaistEntriesService, useValue: waistEntriesService },
                { provide: UserService, useValue: userService },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string) => key),
                        currentLang: 'en',
                        defaultLang: 'en',
                    },
                },
            ],
        });

        facade = TestBed.inject(StatisticsFacade);
    });

    it('loads statistics, body summaries, and user profile on initialize', () => {
        facade.initialize();
        TestBed.flushEffects();

        expect(statisticsService.getAggregatedStatistics).toHaveBeenCalled();
        expect(weightEntriesService.getSummary).toHaveBeenCalled();
        expect(waistEntriesService.getSummary).toHaveBeenCalled();
        expect(userService.getInfo).toHaveBeenCalled();
        expect(facade.chartStatisticsData()?.calories).toEqual([1800]);
        expect(facade.weightSummaryPoints()).toHaveLength(1);
        expect(facade.waistSummaryPoints()).toHaveLength(1);
        expect(facade.userHeightCm()).toBe(180);
    });

    it('reloads aggregated data when the selected range changes', () => {
        facade.initialize();
        TestBed.flushEffects();
        statisticsService.getAggregatedStatistics.mockClear();
        weightEntriesService.getSummary.mockClear();
        waistEntriesService.getSummary.mockClear();

        facade.changeRange('week');
        TestBed.flushEffects();

        expect(statisticsService.getAggregatedStatistics).toHaveBeenCalledTimes(1);
        expect(weightEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(facade.selectedRange()).toBe('week');
    });

    it('marks load error when aggregated statistics request fails', () => {
        statisticsService.getAggregatedStatistics.mockReturnValueOnce(throwError(() => new Error('load failed')));

        facade.initialize();
        TestBed.flushEffects();

        expect(facade.hasLoadError()).toBe(true);
        expect(facade.chartStatisticsData()).toBeNull();
    });

    it('marks body load error when body summaries request fails', () => {
        weightEntriesService.getSummary.mockReturnValueOnce(throwError(() => new Error('body failed')));

        facade.initialize();
        TestBed.flushEffects();

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
                    totalCalories: 2200,
                    averageProteins: 130,
                    averageFats: 75,
                    averageCarbs: 190,
                    averageFiber: 24,
                },
            ]),
        );

        facade.initialize();
        TestBed.flushEffects();

        expect(facade.hasLoadError()).toBe(true);

        facade.reload();
        TestBed.flushEffects();

        expect(facade.hasLoadError()).toBe(false);
        expect(facade.chartStatisticsData()?.calories).toEqual([2200]);
    });
});
