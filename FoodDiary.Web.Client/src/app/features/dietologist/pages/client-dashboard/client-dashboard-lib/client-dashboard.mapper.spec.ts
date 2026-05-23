import { describe, expect, it } from 'vitest';

import { createClient } from '../../clients/dietologist-clients-lib/dietologist-clients.test-data';
import {
    buildBodyTiles,
    buildClientDashboardSections,
    buildClientProfileChips,
    buildClientProfileDetails,
    buildFastingView,
    buildGoalTiles,
    buildHydrationView,
    buildMealViews,
    buildNutritionTiles,
    buildRecommendationViews,
    buildWeightView,
    getClientDashboardTitle,
} from './client-dashboard.mapper';

const EXPECTED_METRIC_TILE_COUNT = 4;

describe('client dashboard mapper', () => {
    it('resolves title from full name or email fallback', () => {
        expect(getClientDashboardTitle(createClient({ firstName: 'Alex', lastName: 'Ivanov' }))).toBe('Alex Ivanov');
        expect(getClientDashboardTitle(createClient({ firstName: null, lastName: null, email: 'client@example.com' }))).toBe(
            'client@example.com',
        );
    });

    it('returns profile chips only when profile sharing is enabled', () => {
        expect(buildClientProfileChips(createClient())).toEqual(['180 cm', 'Male', 'Moderate']);
        expect(buildClientProfileChips(createClient({ permissions: { ...createClient().permissions, shareProfile: false } }))).toEqual([]);
    });

    it('returns profile details only when profile sharing is enabled', () => {
        expect(buildClientProfileDetails(createClient()).map(detail => detail.value)).toContain('client@example.com');
        expect(buildClientProfileDetails(createClient({ permissions: { ...createClient().permissions, shareProfile: false } }))).toEqual(
            [],
        );
    });

    it('returns sections matching shared permissions', () => {
        const sections = buildClientDashboardSections(createClient());

        expect(sections.map(section => section.titleKey)).toEqual([
            'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.PROFILE_TITLE',
            'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.MEALS_TITLE',
        ]);
    });

    it('builds metric tiles from dashboard and goals', () => {
        const snapshot = {
            statistics: {
                totalCalories: 1234,
                averageProteins: 82,
                averageFats: 44,
                averageCarbs: 151,
                averageFiber: 21,
            },
            weight: { latest: { weight: 73 } },
            waist: { latest: { circumference: 84 } },
            hydration: { totalMl: 1500 },
            meals: { total: 4 },
        };

        expect(buildNutritionTiles(snapshot as never)[0]).toEqual({
            labelKey: 'DIETOLOGIST.CLIENT_DASHBOARD.METRICS.CALORIES',
            value: '1234 kcal',
        });
        expect(buildBodyTiles(snapshot as never).map(tile => tile.value)).toEqual(['73 kg', '84 cm', '1500 ml', '4']);
        expect(buildBodyTiles(snapshot as never, createClient().permissions).map(tile => tile.value)).toEqual(['4']);
        expect(buildGoalTiles({ id: 'client-1', email: 'client@example.com', dailyCalorieTarget: 1800 })).toHaveLength(
            EXPECTED_METRIC_TILE_COUNT,
        );
    });
});

describe('client dashboard detail mapper', () => {
    it('maps shared dashboard details for meals, body, hydration and fasting', () => {
        const snapshot = createDashboardSnapshot();

        expect(buildMealViews(snapshot as never)[0]).toEqual(
            expect.objectContaining({
                title: 'Lunch',
                calories: '640 kcal',
                itemSummary: 'Chicken',
            }),
        );
        expect(buildWeightView(snapshot as never)?.delta).toBe('+1.2 kg');
        expect(buildHydrationView(snapshot as never)).toEqual({ total: '1500 ml', goal: '2000 ml', progress: 75 });
        expect(buildFastingView(snapshot as never)).toEqual(
            expect.objectContaining({
                status: 'Active',
                protocol: 'F16_8',
                plannedDuration: '16 h',
            }),
        );
    });

    it('maps recommendations with read state keys', () => {
        expect(
            buildRecommendationViews([
                {
                    id: 'rec-1',
                    dietologistUserId: 'diet-1',
                    dietologistFirstName: null,
                    dietologistLastName: null,
                    text: 'More protein',
                    isRead: true,
                    createdAtUtc: '2026-05-23T00:00:00Z',
                    readAtUtc: '2026-05-23T01:00:00Z',
                },
            ]),
        ).toEqual([
            {
                id: 'rec-1',
                text: 'More protein',
                createdAtUtc: '2026-05-23T00:00:00Z',
                statusKey: 'DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.READ',
            },
        ]);
    });
});

function createDashboardSnapshot(): unknown {
    return {
        date: '2026-05-23T00:00:00Z',
        dailyGoal: 2000,
        weeklyCalorieGoal: 14000,
        statistics: {
            totalCalories: 1234,
            averageProteins: 82,
            averageFats: 44,
            averageCarbs: 151,
            averageFiber: 21,
        },
        weeklyCalories: [],
        weight: { latest: { date: '2026-05-23T00:00:00Z', weight: 73.2 }, previous: { date: '2026-05-22T00:00:00Z', weight: 72 } },
        waist: { latest: { date: '2026-05-23T00:00:00Z', circumference: 84 }, previous: null, desired: 80 },
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
                    items: [{ id: 'item-1', consumptionId: 'meal-1', amount: 150, product: { name: 'Chicken' } }],
                },
            ],
            total: 1,
        },
        hydration: { dateUtc: '2026-05-23T00:00:00Z', totalMl: 1500, goalMl: 2000 },
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
