import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ExportService } from '../../../shared/api/export.service';
import { UserService } from '../../../shared/api/user.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../shared/development/development-tools';
import { StatisticsService } from '../api/statistics.service';
import type { AggregatedStatistics, StatisticsSummary } from '../models/statistics.data';
import { StatisticsFacade } from './statistics.facade';
import { connectStatisticsDevelopmentScreen } from './statistics-development';

const HISTORY_CALORIES = 3850;
const HISTORY_DAYS = 25;
const HISTORY_AVERAGE = 154;
const summary: StatisticsSummary = {
    nutrition: [
        {
            dateFrom: new Date('2025-01-01'),
            dateTo: new Date('2025-01-25'),
            totalCalories: HISTORY_CALORIES,
            averageProteins: 10,
            averageFats: 6,
            averageCarbs: 15,
            averageFiber: 2,
            totalProteins: 250,
            totalFats: 150,
            totalCarbs: 375,
            totalFiber: 50,
            trackedDayCount: HISTORY_DAYS,
            mealCount: HISTORY_DAYS,
            lunchCalories: HISTORY_CALORIES,
            breakfastCalories: 0,
            dinnerCalories: 0,
            snackCalories: 0,
        },
    ],
    weight: [],
    waist: [],
};

function getBodyMeasurements(): ReturnType<StatisticsService['getBodyMeasurements']> {
    return of({
        weightMeasurements: { first: null, last: null, count: 0 },
        waistMeasurements: { first: null, last: null, count: 0 },
    });
}

describe('live statistics audit context', () => {
    let facade: StatisticsFacade;
    let screen: DevelopmentScreen;
    const getSummary = vi.fn();
    beforeEach(() => {
        getSummary.mockReset().mockReturnValue(of(summary));
        TestBed.configureTestingModule({
            providers: [
                StatisticsFacade,
                { provide: StatisticsService, useValue: { getSummary, getBodyMeasurements } },
                { provide: UserService, useValue: { user: signal(null) } },
                { provide: ExportService, useValue: {} },
                {
                    provide: TranslateService,
                    useValue: {
                        onLangChange: new Subject(),
                        getCurrentLang: vi.fn().mockReturnValue('en'),
                        getFallbackLang: vi.fn().mockReturnValue('en'),
                    },
                },
                {
                    provide: DEVELOPMENT_SCREENS,
                    useValue: {
                        attach: (value: DevelopmentScreen): (() => void) => {
                            screen = value;
                            return (): void => {};
                        },
                    },
                },
            ],
        });
        facade = TestBed.inject(StatisticsFacade);
        TestBed.runInInjectionContext(() => {
            connectStatisticsDevelopmentScreen(facade);
        });
        facade.initialize();
        TestBed.tick();
        screen.setFilters?.({ dateFrom: '2025-01-01', dateTo: '2025-01-25' });
        TestBed.tick();
    });
    it('uses owner recorded-day aggregates instead of treating one bucket as one recorded day', () => {
        expect(screen.read()).toMatchObject({
            range: 'custom',
            available: true,
            period: { dateFrom: '2025-01-01', dateTo: '2025-01-25' },
            aggregates: { trackedDays: HISTORY_DAYS, totalCalories: HISTORY_CALORIES, averageCalories: HISTORY_AVERAGE },
            units: { calories: 'kcal', nutrients: 'g' },
        });
    });
    it('reports one recorded day when a zero-calorie meal shares a period with an empty day', () => {
        const nutrition = [calorieFreeDay('2025-02-01', 0), calorieFreeDay('2025-02-02', 1)];
        getSummary.mockReturnValueOnce(of({ ...summary, nutrition }));
        screen.setFilters?.({ dateFrom: '2025-02-01', dateTo: '2025-02-02' });
        TestBed.tick();

        expect(screen.read()).toMatchObject({ available: true, aggregates: { trackedDays: 1, totalCalories: 0, averageCalories: 0 } });
    });
    it('hides old aggregates while a new period is loading or has failed', () => {
        const pending = new Subject<StatisticsSummary>();
        getSummary.mockReturnValueOnce(pending);
        screen.setFilters?.({ dateFrom: '2025-02-01', dateTo: '2025-02-10' });
        TestBed.tick();
        expect(screen.read()).toMatchObject({
            loading: true,
            available: false,
            aggregates: null,
            appliedPeriod: { dateFrom: '2025-01-01', dateTo: '2025-01-25' },
        });
        pending.error(new Error('failure'));
        TestBed.tick();
        expect(screen.read()).toMatchObject({ loading: false, error: true, available: false, aggregates: null });
    });
    it('rejects an invalid range before changing any selected filter', () => {
        const original = facade.currentRange();
        expect(() => screen.setFilters?.({ dateFrom: '2025-03-10', dateTo: '2025-03-01', nutritionTab: 'macros' })).toThrow();
        expect(facade.currentRange()).toEqual(original);
        expect(facade.selectedNutritionTab()).toBe('calories');
    });
});

function calorieFreeDay(day: string, mealCount: 0 | 1): AggregatedStatistics {
    return {
        dateFrom: new Date(`${day}T00:00:00`),
        dateTo: new Date(`${day}T23:59:59`),
        totalCalories: 0,
        averageProteins: 0,
        averageFats: 0,
        averageCarbs: 0,
        averageFiber: 0,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        mealCount,
        trackedDayCount: mealCount,
    };
}
