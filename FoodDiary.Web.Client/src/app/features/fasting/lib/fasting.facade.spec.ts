import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { UserService } from '../../../shared/api/user.service';
import { FastingService } from '../api/fasting.service';
import type { FastingOverview, FastingSession } from '../models/fasting.data';
import { FastingFacade } from './fasting.facade';

const DEFAULT_FASTING_HOURS = 16;
const DEFAULT_EXTEND_HOURS = 24;
const CUSTOM_REDUCE_HOURS = 8;
const CUSTOM_EXTENDED_HOURS = 48;
const CUSTOM_RESTORED_HOURS = 72;
const REDUCED_PLANNED_HOURS = 28;
const REMINDER_HOURS = 12;
const FOLLOW_UP_REMINDER_HOURS = 20;
const HISTORY_PAGE = 2;
const HISTORY_TOTAL_ITEMS = 11;
const COMPLETION_RATE = 50;
const CHECK_IN_RATE = 25;
const HUNGER_LEVEL = 2;
const ENERGY_LEVEL = 4;
const MOOD_LEVEL = 4;
const EXTENDED_PROTOCOL_HOURS = 36;

describe('FastingFacade', () => {
    let facade: FastingFacade;
    let fastingService: {
        getOverview: ReturnType<typeof vi.fn>;
        getHistory: ReturnType<typeof vi.fn>;
        updateCheckIn: ReturnType<typeof vi.fn>;
        end: ReturnType<typeof vi.fn>;
        start: ReturnType<typeof vi.fn>;
        extend: ReturnType<typeof vi.fn>;
        reduceTarget: ReturnType<typeof vi.fn>;
        skipCyclicDay: ReturnType<typeof vi.fn>;
        postponeCyclicDay: ReturnType<typeof vi.fn>;
    };
    let frontendObservability: { recordFastingLifecycleEvent: ReturnType<typeof vi.fn> };
    let userService: { user: ReturnType<typeof vi.fn> };

    const activeSession: FastingSession = {
        id: 'session-1',
        startedAtUtc: '2026-04-12T06:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: DEFAULT_FASTING_HOURS,
        addedDurationHours: 0,
        plannedDurationHours: DEFAULT_FASTING_HOURS,
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
    };

    const baseOverview: FastingOverview = {
        currentSession: null,
        stats: {
            totalCompleted: 2,
            currentStreak: 1,
            averageDurationHours: DEFAULT_FASTING_HOURS,
            completionRateLast30Days: COMPLETION_RATE,
            checkInRateLast30Days: CHECK_IN_RATE,
            lastCheckInAtUtc: null,
            topSymptom: null,
        },
        insights: {
            alerts: [],
            insights: [],
        },
        history: {
            data: [],
            page: 1,
            limit: 10,
            totalPages: 0,
            totalItems: 0,
        },
    };

    beforeEach(() => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-04-13T09:45:00Z'));

        fastingService = {
            getOverview: vi.fn().mockReturnValue(of(baseOverview)),
            getHistory: vi
                .fn()
                .mockReturnValue(
                    of({ data: [], page: HISTORY_PAGE, limit: 10, totalPages: HISTORY_PAGE, totalItems: HISTORY_TOTAL_ITEMS }),
                ),
            updateCheckIn: vi.fn(),
            end: vi.fn(),
            start: vi.fn(),
            extend: vi.fn(),
            reduceTarget: vi.fn(),
            skipCyclicDay: vi.fn(),
            postponeCyclicDay: vi.fn(),
        };
        frontendObservability = {
            recordFastingLifecycleEvent: vi.fn(),
        };
        userService = {
            user: vi.fn(() => ({
                fastingCheckInReminderHours: REMINDER_HOURS,
                fastingCheckInFollowUpReminderHours: FOLLOW_UP_REMINDER_HOURS,
            })),
        };

        localStorage.clear();

        TestBed.configureTestingModule({
            providers: [
                FastingFacade,
                { provide: FastingService, useValue: fastingService },
                { provide: FrontendObservabilityService, useValue: frontendObservability },
                { provide: UserService, useValue: userService },
            ],
        });

        facade = TestBed.inject(FastingFacade);
    });

    afterEach(() => {
        vi.clearAllTimers();
        vi.useRealTimers();
    });

    it('initializes from overview bootstrap', () => {
        const overview: FastingOverview = {
            ...baseOverview,
            currentSession: activeSession,
            history: {
                data: [activeSession],
                page: 1,
                limit: 10,
                totalPages: HISTORY_PAGE,
                totalItems: HISTORY_TOTAL_ITEMS,
            },
        };
        fastingService.getOverview.mockReturnValueOnce(of(overview));

        facade.initialize();

        expect(fastingService.getOverview).toHaveBeenCalledTimes(1);
        expect(facade.currentSession()).toEqual(activeSession);
        expect(facade.history()).toEqual([activeSession]);
        expect(facade.historyPage()).toBe(1);
        expect(facade.historyTotalPages()).toBe(2);
        expect(facade.isLoading()).toBe(false);
    });

    it('loads more history and appends next page', () => {
        fastingService.getOverview.mockReturnValueOnce(
            of({
                ...baseOverview,
                history: {
                    data: [activeSession],
                    page: 1,
                    limit: 10,
                    totalPages: 2,
                    totalItems: 11,
                },
            }),
        );
        const olderSession = { ...activeSession, id: 'session-2', startedAtUtc: '2026-04-11T06:00:00Z' };
        fastingService.getHistory.mockReturnValueOnce(
            of({
                data: [olderSession],
                page: HISTORY_PAGE,
                limit: 10,
                totalPages: HISTORY_PAGE,
                totalItems: HISTORY_TOTAL_ITEMS,
            }),
        );

        facade.initialize();
        facade.loadMoreHistory();

        expect(fastingService.getHistory).toHaveBeenCalledWith(
            expect.objectContaining({
                from: '2026-03-01T00:00:00.000Z',
                to: '2026-05-31T23:59:59.999Z',
                page: HISTORY_PAGE,
                limit: 10,
            }),
        );
        expect(facade.history()).toEqual([activeSession, olderSession]);
        expect(facade.historyPage()).toBe(HISTORY_PAGE);
    });

    it('saves check-in, increments version, and refreshes overview', () => {
        facade.currentSession.set(activeSession);
        fastingService.updateCheckIn.mockReturnValueOnce(
            of({
                ...activeSession,
                checkInAtUtc: '2026-04-12T10:00:00Z',
                hungerLevel: HUNGER_LEVEL,
                energyLevel: ENERGY_LEVEL,
                moodLevel: MOOD_LEVEL,
                symptoms: ['weakness'],
                checkInNotes: 'steady',
                checkIns: [
                    {
                        id: 'checkin-1',
                        checkedInAtUtc: '2026-04-12T10:00:00Z',
                        hungerLevel: HUNGER_LEVEL,
                        energyLevel: ENERGY_LEVEL,
                        moodLevel: MOOD_LEVEL,
                        symptoms: ['weakness'],
                        notes: 'steady',
                    },
                ],
            }),
        );
        fastingService.getOverview.mockReturnValueOnce(
            of({
                ...baseOverview,
                currentSession: {
                    ...activeSession,
                    checkInAtUtc: '2026-04-12T10:00:00Z',
                    hungerLevel: HUNGER_LEVEL,
                    energyLevel: ENERGY_LEVEL,
                    moodLevel: MOOD_LEVEL,
                    symptoms: ['weakness'],
                    checkInNotes: 'steady',
                    checkIns: [
                        {
                            id: 'checkin-1',
                            checkedInAtUtc: '2026-04-12T10:00:00Z',
                            hungerLevel: HUNGER_LEVEL,
                            energyLevel: ENERGY_LEVEL,
                            moodLevel: MOOD_LEVEL,
                            symptoms: ['weakness'],
                            notes: 'steady',
                        },
                    ],
                },
            }),
        );

        facade.setHungerLevel(HUNGER_LEVEL);
        facade.setEnergyLevel(ENERGY_LEVEL);
        facade.setMoodLevel(MOOD_LEVEL);
        facade.toggleSymptom('weakness');
        facade.setCheckInNotes('steady');
        facade.saveCheckIn();

        expect(fastingService.updateCheckIn).toHaveBeenCalledWith({
            hungerLevel: HUNGER_LEVEL,
            energyLevel: ENERGY_LEVEL,
            moodLevel: MOOD_LEVEL,
            symptoms: ['weakness'],
            checkInNotes: 'steady',
        });
        expect(fastingService.getOverview).toHaveBeenCalledTimes(1);
        expect(facade.checkInSavedVersion()).toBe(1);
        expect(frontendObservability.recordFastingLifecycleEvent).toHaveBeenCalledWith(
            'check-in.saved',
            expect.objectContaining({
                sessionId: 'session-1',
                hungerLevel: HUNGER_LEVEL,
                energyLevel: ENERGY_LEVEL,
                moodLevel: MOOD_LEVEL,
            }),
        );
    });

    it('ends fasting and resets draft state when overview returns idle state', () => {
        facade.currentSession.set(activeSession);
        facade.selectMode('cyclic');
        facade.selectProtocol('F20_4');
        facade.setCustomHours(CUSTOM_EXTENDED_HOURS);
        fastingService.end.mockReturnValueOnce(
            of({
                ...activeSession,
                endedAtUtc: '2026-04-12T12:00:00Z',
                status: 'Completed',
                isCompleted: true,
            }),
        );
        fastingService.getOverview.mockReturnValueOnce(of(baseOverview));

        facade.endFasting();

        expect(fastingService.end).toHaveBeenCalledTimes(1);
        expect(fastingService.getOverview).toHaveBeenCalledTimes(1);
        expect(facade.currentSession()).toBeNull();
        expect(facade.selectedMode()).toBe('intermittent');
        expect(facade.selectedProtocol()).toBe('F16_8');
        expect(facade.extendHours()).toBe(DEFAULT_EXTEND_HOURS);
    });

    it('keeps selected protocol valid when switching between setup modes', () => {
        facade.selectProtocol('F20_4');
        facade.selectMode('extended');

        expect(facade.selectedProtocol()).toBe('F24_0');

        facade.selectMode('intermittent');

        expect(facade.selectedProtocol()).toBe('F16_8');

        facade.selectProtocol('F18_6');
        facade.selectMode('cyclic');
        facade.selectMode('intermittent');

        expect(facade.selectedProtocol()).toBe('F18_6');
    });

    it('restores an intermittent protocol after switching from custom extended setup', () => {
        facade.selectMode('extended');
        facade.selectProtocol('Custom');
        facade.setCustomHours(CUSTOM_RESTORED_HOURS);

        facade.selectMode('intermittent');

        expect(facade.selectedProtocol()).toBe('F16_8');
        expect(facade.plannedDurationHours()).toBe(DEFAULT_FASTING_HOURS);
    });

    it('reduces target locally without overview refresh', () => {
        facade.currentSession.set({
            ...activeSession,
            protocol: 'F36_0',
            planType: 'Extended',
            occurrenceKind: 'FastDay',
            initialPlannedDurationHours: EXTENDED_PROTOCOL_HOURS,
            plannedDurationHours: EXTENDED_PROTOCOL_HOURS,
        });
        fastingService.reduceTarget.mockReturnValueOnce(
            of({
                ...activeSession,
                protocol: 'F36_0',
                planType: 'Extended',
                occurrenceKind: 'FastDay',
                initialPlannedDurationHours: EXTENDED_PROTOCOL_HOURS,
                addedDurationHours: -CUSTOM_REDUCE_HOURS,
                plannedDurationHours: REDUCED_PLANNED_HOURS,
            }),
        );
        facade.reduceTargetByHours(CUSTOM_REDUCE_HOURS);

        expect(fastingService.reduceTarget).toHaveBeenCalledWith({ reducedHours: CUSTOM_REDUCE_HOURS });
        expect(fastingService.getOverview).not.toHaveBeenCalled();
        expect(facade.currentSession()?.plannedDurationHours).toBe(REDUCED_PLANNED_HOURS);
    });
});
