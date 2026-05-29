import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DietologistService } from '../../api/dietologist.service';
import { createClient } from '../clients/dietologist-clients-lib/dietologist-clients.test-data';
import { ClientDashboardComponent } from './client-dashboard.component';

let fixture: ComponentFixture<ClientDashboardComponent>;
let component: ClientDashboardComponent;
const EXPECTED_METRIC_TILE_COUNT = 4;
const SELECTED_YEAR = 2026;
const SELECTED_DAY = 22;
const PERIOD_START_DAY = 16;
const MAY_MONTH_INDEX = 4;
let dietologistService: {
    getMyClients: ReturnType<typeof vi.fn>;
    getClientDashboard: ReturnType<typeof vi.fn>;
    getClientGoals: ReturnType<typeof vi.fn>;
    getRecommendationsForClient: ReturnType<typeof vi.fn>;
    createRecommendation: ReturnType<typeof vi.fn>;
    disconnectClient: ReturnType<typeof vi.fn>;
};
let router: { navigate: ReturnType<typeof vi.fn> };
let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };

beforeEach(() => {
    dietologistService = {
        getMyClients: vi.fn(() => of([createClient({ userId: 'client-1' })])),
        getClientDashboard: vi.fn(() => of(createDashboardSnapshot())),
        getClientGoals: vi.fn(() => of({ id: 'client-1', email: 'client@example.com', dailyCalorieTarget: 1800 })),
        getRecommendationsForClient: vi.fn(() => of([])),
        createRecommendation: vi.fn(() =>
            of({
                id: 'rec-1',
                dietologistUserId: 'diet-1',
                dietologistFirstName: null,
                dietologistLastName: null,
                text: 'Add protein',
                isRead: false,
                createdAtUtc: '2026-05-23T00:00:00Z',
                readAtUtc: null,
            }),
        ),
        disconnectClient: vi.fn(() => of(undefined)),
    };
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
    toastService = {
        success: vi.fn(),
        error: vi.fn(),
        info: vi.fn(),
    };
});

describe('ClientDashboardComponent', () => {
    registerLoadingTests();
    registerActionTests();
});

function registerLoadingTests(): void {
    it('loads selected client by route id', () => {
        createComponent('client-1');

        expect(component['loading']()).toBe(false);
        expect(component['clientTitle']()).toBe('Alex Ivanov');
        expect(component['profileChips']()).toContain('180 cm');
        expect(component['visibleSections']()).toHaveLength(2);
    });

    it('sets empty client when route id does not match', () => {
        createComponent('missing-client');

        expect(component['loading']()).toBe(false);
        expect(component['client']()).toBeNull();
    });

    it('stops loading on request error', () => {
        dietologistService.getMyClients.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent('client-1');

        expect(component['loading']()).toBe(false);
        expect(component['client']()).toBeNull();
    });

    it('navigates back to clients list', () => {
        createComponent('client-1');

        component['goBack']();

        expect(router.navigate).toHaveBeenCalledWith(['/dietologist']);
    });

    it('loads shared dashboard and goals when permissions allow it', () => {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([
                createClient({
                    userId: 'client-1',
                    permissions: { ...createClient().permissions, shareStatistics: true, shareGoals: true },
                }),
            ]),
        );

        createComponent('client-1');

        expect(dietologistService.getClientDashboard).toHaveBeenCalledWith('client-1', expect.objectContaining({ trendDays: 14 }));
        expect(dietologistService.getClientGoals).toHaveBeenCalledWith('client-1');
        expect(component['nutritionTiles']()).toHaveLength(EXPECTED_METRIC_TILE_COUNT);
        expect(component['goalTiles']()).toHaveLength(EXPECTED_METRIC_TILE_COUNT);
    });

    it('hides period filter when only profile and goals are shared', () => {
        dietologistService.getMyClients.mockReturnValueOnce(
            of([
                createClient({
                    userId: 'client-1',
                    permissions: {
                        ...createClient().permissions,
                        shareMeals: false,
                        shareGoals: true,
                    },
                }),
            ]),
        );

        createComponent('client-1');

        expect(component['hasAnyPermission']()).toBe(true);
        expect(component['hasPeriodFilterPermission']()).toBe(false);
        expect(dietologistService.getClientDashboard).not.toHaveBeenCalled();
        expect(dietologistService.getClientGoals).toHaveBeenCalledWith('client-1');
    });

    it('loads meal details when meal sharing is enabled', () => {
        createComponent('client-1');

        expect(dietologistService.getClientDashboard).toHaveBeenCalledWith('client-1', expect.objectContaining({ trendDays: 14 }));
        expect(component['nutritionTiles']()).toEqual([]);
        expect(component['mealItems']()[0]).toEqual(expect.objectContaining({ id: 'meal-1', title: 'Lunch', calories: '640 kcal' }));
        expect(component['bodyTiles']().map(tile => tile.value)).toEqual(['1']);
    });
}

function registerActionTests(): void {
    it('reloads dashboard for selected period', () => {
        createComponent('client-1');

        component['dateFilterForm'].setValue({ dateFrom: '2026-05-16', dateTo: '2026-05-22' });
        component['applyDateFilter']();

        expect(dietologistService.getClientDashboard).toHaveBeenCalledTimes(2);
        expect(getLastDashboardPeriod()).toEqual(
            expect.objectContaining({
                dateFrom: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, PERIOD_START_DAY),
                dateTo: new Date(SELECTED_YEAR, MAY_MONTH_INDEX, SELECTED_DAY),
            }),
        );
    });

    it('moves dashboard dates with period navigation', () => {
        createComponent('client-1');

        component['dateFilterForm'].setValue({ dateFrom: '2026-05-17', dateTo: '2026-05-23' });
        component['applyDateFilter']();
        component['showPreviousPeriod']();

        expect(component['selectedDateFrom']()).toBe('2026-05-10');
        expect(component['selectedDateTo']()).toBe('2026-05-16');
    });

    it('shows section load warning when optional details fail', () => {
        dietologistService.getClientDashboard.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent('client-1');

        expect(component['sectionLoadError']()).toBe('DIETOLOGIST.CLIENT_DASHBOARD.PARTIAL_LOAD_ERROR');
        expect(component['dashboard']()).toBeNull();
    });

    it('sends recommendation and prepends it to the list', () => {
        createComponent('client-1');

        component['recommendationForm'].controls.text.setValue('Add protein');
        component['submitRecommendation']();

        expect(dietologistService.createRecommendation).toHaveBeenCalledWith('client-1', { text: 'Add protein' });
        expect(component['recommendations']()[0]?.id).toBe('rec-1');
        expect(toastService.success).toHaveBeenCalled();
    });

    it('disconnects selected client', () => {
        createComponent('client-1');

        component['disconnectClient']();

        expect(dietologistService.disconnectClient).toHaveBeenCalledWith('client-1');
        expect(router.navigate).toHaveBeenCalledWith(['/dietologist']);
    });
}

function createComponent(clientId: string): void {
    TestBed.configureTestingModule({
        imports: [ClientDashboardComponent, TranslateModule.forRoot()],
        providers: [
            { provide: DietologistService, useValue: dietologistService },
            { provide: Router, useValue: router },
            { provide: FdUiToastService, useValue: toastService },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        paramMap: convertToParamMap({ clientId }),
                    },
                },
            },
        ],
    });

    fixture = TestBed.createComponent(ClientDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

function createDashboardSnapshot(): unknown {
    return {
        date: '2026-05-23T00:00:00Z',
        dateTo: '2026-05-23T00:00:00Z',
        dailyGoal: 2000,
        weeklyCalorieGoal: 14000,
        statistics: {
            totalCalories: 1200,
            averageProteins: 80,
            averageFats: 45,
            averageCarbs: 150,
            averageFiber: 20,
        },
        weeklyCalories: [],
        weight: {
            latest: { date: '2026-05-23T00:00:00Z', weight: 72 },
            previous: { date: '2026-05-22T00:00:00Z', weight: 73 },
            desired: 70,
        },
        waist: {
            latest: { date: '2026-05-23T00:00:00Z', circumference: 82 },
            previous: { date: '2026-05-22T00:00:00Z', circumference: 83 },
            desired: 80,
        },
        meals: {
            items: [
                {
                    id: 'meal-1',
                    date: '2026-05-23T12:30:00Z',
                    mealType: 'Lunch',
                    comment: null,
                    totalCalories: 640,
                    totalProteins: 42,
                    totalFats: 22,
                    totalCarbs: 64,
                    totalFiber: 8,
                    totalAlcohol: 0,
                    isNutritionAutoCalculated: true,
                    items: [{ id: 'item-1', consumptionId: 'meal-1', amount: 150, sourceType: 'Product', product: { name: 'Chicken' } }],
                },
            ],
            total: 1,
        },
        hydration: { dateUtc: '2026-05-23T00:00:00Z', totalMl: 1200, goalMl: 2000 },
        currentFastingSession: {
            id: 'fast-1',
            startedAtUtc: '2026-05-23T00:00:00Z',
            endedAtUtc: null,
            initialPlannedDurationHours: 16,
            addedDurationHours: 0,
            plannedDurationHours: 16,
            protocol: 'F16_8',
            planType: 'Intermittent',
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
        },
    };
}

function getLastDashboardPeriod(): { dateFrom: Date; dateTo?: Date } | undefined {
    const calls = dietologistService.getClientDashboard.mock.calls;
    const lastCall = calls.at(-1) as [string, { dateFrom: Date; dateTo?: Date }] | undefined;
    return lastCall?.[1];
}
