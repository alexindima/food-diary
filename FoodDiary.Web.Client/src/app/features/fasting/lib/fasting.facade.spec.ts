import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { UserService } from '../../../shared/api/user.service';
import { FastingService } from '../api/fasting.service';
import type { FastingMessage, FastingOverview, FastingSession } from '../models/fasting.data';
import { FastingFacade } from './fasting.facade';

const OUT_OF_RANGE_DURATION = 100;
const MAX_INTERVAL_HOURS = 23;
const EATING_DAYS = 3;
const LONGER_INTERVAL_HOURS = 18;
const EXCESS_INTERVAL_HOURS = 50;
const MINUTES_PER_HOUR = 60;
const TIMER_TICK_MS = 1000;
const INITIAL_PROGRESS_PERCENT = 12.5;

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

let facade: FastingFacade;
let fastingService: FastingServiceMock;
let frontendObservability: { recordFastingLifecycleEvent: ReturnType<typeof vi.fn> };
let userService: { user: ReturnType<typeof vi.fn> };
let activeSession: FastingSession;
let baseOverview: FastingOverview;

describe('FastingFacade overview history', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);

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
});

describe('FastingFacade check-ins', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);

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
        fastingService.getOverviewStrict.mockReturnValueOnce(
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
        expect(fastingService.getOverviewStrict).toHaveBeenCalledTimes(1);
        expect(facade.currentSession()?.checkInAtUtc).toBe('2026-04-12T10:00:00Z');
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
});

describe('FastingFacade check-in refresh failures', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);

    it('keeps saved check-in visible when overview refresh fails', () => {
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
            }),
        );
        fastingService.getOverviewStrict.mockReturnValueOnce(throwError(() => new Error('refresh failed')));

        facade.setHungerLevel(HUNGER_LEVEL);
        facade.setEnergyLevel(ENERGY_LEVEL);
        facade.setMoodLevel(MOOD_LEVEL);
        facade.toggleSymptom('weakness');
        facade.setCheckInNotes('steady');
        facade.saveCheckIn();

        expect(facade.currentSession()?.checkInAtUtc).toBe('2026-04-12T10:00:00Z');
        expect(facade.currentSession()?.symptoms).toEqual(['weakness']);
        expect(facade.checkInSavedVersion()).toBe(1);
    });
});

describe('FastingFacade session completion', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);

    it('ends fasting and resets draft state when overview returns idle state', () => {
        facade.currentSession.set(activeSession);
        facade.selectMode('cyclic');
        facade.selectProtocol('Fast20Eat4');
        facade.setCustomHours(CUSTOM_EXTENDED_HOURS);
        fastingService.end.mockReturnValueOnce(
            of({
                ...activeSession,
                endedAtUtc: '2026-04-12T12:00:00Z',
                status: 'Completed',
                isCompleted: true,
            }),
        );
        fastingService.getOverviewStrict.mockReturnValueOnce(of(baseOverview));

        facade.endFasting();

        expect(fastingService.end).toHaveBeenCalledTimes(1);
        expect(fastingService.getOverviewStrict).toHaveBeenCalledTimes(1);
        expect(facade.currentSession()).toBeNull();
        expect(facade.selectedMode()).toBe('intermittent');
        expect(facade.selectedProtocol()).toBe('Fast16Eat8');
        expect(facade.extendHours()).toBe(DEFAULT_EXTEND_HOURS);
    });

    it('keeps completed state when overview refresh fails after ending fasting', () => {
        facade.currentSession.set(activeSession);
        facade.selectMode('cyclic');
        fastingService.end.mockReturnValueOnce(
            of({
                ...activeSession,
                endedAtUtc: '2026-04-12T12:00:00Z',
                status: 'Completed',
                isCompleted: true,
            }),
        );
        fastingService.getOverviewStrict.mockReturnValueOnce(throwError(() => new Error('refresh failed')));

        facade.endFasting();

        expect(facade.currentSession()).toBeNull();
        expect(facade.selectedMode()).toBe('intermittent');
    });
});

describe('FastingFacade session start and cycle actions', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);

    it('shows started session even when overview refresh fails', () => {
        fastingService.start.mockReturnValueOnce(of(activeSession));
        fastingService.getOverviewStrict.mockReturnValueOnce(throwError(() => new Error('refresh failed')));

        facade.startFasting();

        expect(fastingService.start).toHaveBeenCalledTimes(1);
        expect(fastingService.getOverviewStrict).toHaveBeenCalledTimes(1);
        expect(facade.currentSession()).toEqual(activeSession);
        expect(frontendObservability.recordFastingLifecycleEvent).toHaveBeenCalledWith(
            'session.started',
            expect.objectContaining({ sessionId: activeSession.id }),
        );
    });

    it('applies cyclic day updates before refreshing overview', () => {
        const nextSession = {
            ...activeSession,
            id: 'session-2',
            occurrenceKind: 'EatingWindow',
            cyclicPhaseDayNumber: 2,
        } satisfies FastingSession;
        fastingService.skipCyclicDay.mockReturnValueOnce(of(nextSession));
        fastingService.getOverviewStrict.mockReturnValueOnce(throwError(() => new Error('refresh failed')));

        facade.skipCyclicDay();

        expect(fastingService.skipCyclicDay).toHaveBeenCalledTimes(1);
        expect(facade.currentSession()).toEqual(nextSession);
    });
});

describe('FastingFacade setup modes and target changes', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);

    it('keeps selected protocol valid when switching between setup modes', () => {
        facade.selectProtocol('Fast20Eat4');
        facade.selectMode('extended');

        expect(facade.selectedProtocol()).toBe('Fast24');

        facade.selectMode('intermittent');

        expect(facade.selectedProtocol()).toBe('Fast16Eat8');

        facade.selectProtocol('Fast18Eat6');
        facade.selectMode('cyclic');
        facade.selectMode('intermittent');

        expect(facade.selectedProtocol()).toBe('Fast18Eat6');
    });

    it('restores an intermittent protocol after switching from custom extended setup', () => {
        facade.selectMode('extended');
        facade.selectProtocol('Custom');
        facade.setCustomHours(CUSTOM_RESTORED_HOURS);

        facade.selectMode('intermittent');

        expect(facade.selectedProtocol()).toBe('Fast16Eat8');
        expect(facade.plannedDurationHours()).toBe(DEFAULT_FASTING_HOURS);
    });

    it('reduces target locally without overview refresh', () => {
        facade.currentSession.set({
            ...activeSession,
            protocol: 'Fast36',
            planType: 'Extended',
            occurrenceKind: 'FastDay',
            initialPlannedDurationHours: EXTENDED_PROTOCOL_HOURS,
            plannedDurationHours: EXTENDED_PROTOCOL_HOURS,
        });
        fastingService.reduceTarget.mockReturnValueOnce(
            of({
                ...activeSession,
                protocol: 'Fast36',
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

    it('does not reduce target beyond the remaining full hours', () => {
        facade.currentSession.set({
            ...activeSession,
            startedAtUtc: '2026-04-12T10:00:00Z',
            protocol: 'F24',
            planType: 'Extended',
            occurrenceKind: 'FastDay',
            initialPlannedDurationHours: DEFAULT_EXTEND_HOURS,
            plannedDurationHours: DEFAULT_EXTEND_HOURS,
        });

        expect(facade.maxReducibleHours()).toBe(0);

        facade.reduceTargetByHours(CUSTOM_REDUCE_HOURS);

        expect(fastingService.reduceTarget).not.toHaveBeenCalled();
    });
});

describe('FastingFacade lifecycle regression (1)', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);
    it('clamps custom intervals and cyclic days before starting', () => {
        facade.selectMode('intermittent');
        facade.selectProtocol('CustomIntermittent');
        facade.setCustomIntermittentFastHours(0);
        expect(facade.plannedDurationHours()).toBe(1);
        facade.setCustomIntermittentFastHours(OUT_OF_RANGE_DURATION);
        expect(facade.plannedDurationHours()).toBe(MAX_INTERVAL_HOURS);
        facade.setCyclicPreset(2, EATING_DAYS);
        expect(facade.cyclicUsesCustomPreset()).toBe(false);
        expect([facade.cyclicFastDays(), facade.cyclicEatDays()]).toEqual([2, EATING_DAYS]);
        facade.selectCustomCyclicPreset();
        facade.setCyclicFastDays(0);
        facade.setCyclicEatDays(OUT_OF_RANGE_DURATION);
        expect(facade.cyclicUsesCustomPreset()).toBe(true);
        expect(facade.cyclicFastDays()).toBe(1);
        expect(facade.cyclicEatDays()).toBeLessThan(OUT_OF_RANGE_DURATION);
        facade.selectCyclicEatDayProtocol('Fast18Eat6');
        expect(facade.cyclicEatDayFastHours()).toBe(LONGER_INTERVAL_HOURS);
        facade.selectCyclicEatDayProtocol('CustomIntermittent');
        expect(facade.cyclicEatDayFastHours()).toBe(LONGER_INTERVAL_HOURS);
        facade.setCyclicEatDayFastHours(EXCESS_INTERVAL_HOURS);
        expect(facade.cyclicEatDayProtocol()).toBe('CustomIntermittent');
        expect(facade.cyclicEatDayFastHours()).toBe(MAX_INTERVAL_HOURS);
    });
    it('updates elapsed and remaining times and stops the timer after destruction', () => {
        const startedAtUtc = new Date(Date.now() - 2 * MINUTES_PER_HOUR * MINUTES_PER_HOUR * TIMER_TICK_MS).toISOString();
        fastingService.getOverview.mockReturnValueOnce(of({ ...baseOverview, currentSession: { ...activeSession, startedAtUtc } }));
        facade.initialize();
        expect(facade.isActive()).toBe(true);
        expect(facade.elapsedFormatted()).toBe('02:00:00');
        expect(facade.remainingFormatted()).toBe('14:00:00');
        expect(facade.progressPercent()).toBeCloseTo(INITIAL_PROGRESS_PERCENT);
        expect(facade.isOvertime()).toBe(false);
        vi.advanceTimersByTime(TIMER_TICK_MS);
        expect(facade.elapsedFormatted()).toBe('02:00:01');
        TestBed.resetTestingModule();
        const last = facade.now();
        vi.advanceTimersByTime(TIMER_TICK_MS);
        expect(facade.now()).toBe(last);
    });
});

describe('FastingFacade lifecycle regression (2)', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);
    it('extends the active session and replaces its history entry', () => {
        facade.currentSession.set(activeSession);
        facade.history.set([activeSession]);
        const updated = { ...activeSession, plannedDurationHours: 24, addedDurationHours: 8 };
        fastingService.extend.mockReturnValueOnce(of(updated));
        facade.setExtendHours(CUSTOM_REDUCE_HOURS);
        facade.extendByHours(facade.extendHours());
        expect(fastingService.extend).toHaveBeenCalledWith({ additionalHours: 8 });
        expect(facade.currentSession()).toEqual(updated);
        expect(facade.history()).toEqual([updated]);
        expect(facade.isExtending()).toBe(false);
    });
    it('preserves the session after failed extension and releases loading', () => {
        facade.currentSession.set(activeSession);
        fastingService.extend.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.extendByHours(CUSTOM_REDUCE_HOURS);
        expect(facade.currentSession()).toEqual(activeSession);
        expect(facade.isExtending()).toBe(false);
    });
});

describe('FastingFacade lifecycle regression (3)', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);
    it.each(['skip', 'postpone'] as const)('updates the cyclic day after %s', action => {
        const updated = { ...activeSession, planType: 'Cyclic' as const, cyclicPhaseDayNumber: 2 };
        fastingService.skipCyclicDay.mockReturnValueOnce(of(updated));
        fastingService.postponeCyclicDay.mockReturnValueOnce(of(updated));
        fastingService.getOverviewStrict.mockReturnValueOnce(of({ ...baseOverview, currentSession: updated }));
        if (action === 'skip') {
            facade.skipCyclicDay();
        } else {
            facade.postponeCyclicDay();
        }
        expect(facade.currentSession()).toEqual(updated);
        expect(fastingService.getOverviewStrict).toHaveBeenCalledTimes(1);
        expect(facade.isUpdatingCycle()).toBe(false);
    });
    it('cancels pending cycle mutations on destruction', () => {
        const pending = new Subject<FastingSession>();
        fastingService.postponeCyclicDay.mockReturnValueOnce(pending);
        facade.postponeCyclicDay();
        expect(facade.isUpdatingCycle()).toBe(true);
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        expect(facade.isUpdatingCycle()).toBe(false);
        expect(fastingService.getOverviewStrict).not.toHaveBeenCalled();
    });
});

describe('FastingFacade lifecycle regression (4)', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);
    it('toggles symptoms and resets check-in drafts from the confirmed session', () => {
        facade.currentSession.set({ ...activeSession, symptoms: ['headache'], checkInNotes: 'saved' });
        facade.toggleSymptom('fatigue');
        expect(facade.selectedSymptoms()).toContain('fatigue');
        facade.toggleSymptom('fatigue');
        expect(facade.selectedSymptoms()).not.toContain('fatigue');
        facade.setCheckInNotes('draft');
        facade.resetCheckInDraft();
        expect(facade.selectedSymptoms()).toEqual(['headache']);
        expect(facade.checkInNotes()).toBe('saved');
    });
    it('does not save a check-in without an active session', () => {
        facade.saveCheckIn();
        facade.currentSession.set({ ...activeSession, endedAtUtc: new Date().toISOString() });
        facade.saveCheckIn();
        expect(fastingService.updateCheckIn).not.toHaveBeenCalled();
    });
});

describe('FastingFacade prompt visibility', () => {
    beforeEach(setupFacade);
    afterEach(teardownFacade);
    const prompt: FastingMessage = { id: 'check-in', titleKey: 'TITLE', bodyKey: 'BODY', tone: 'neutral', bodyParams: null };

    it('hides prompts without a running session or a message', () => {
        expect(facade.isPromptVisible(null, prompt)).toBe(false);
        expect(facade.isPromptVisible(activeSession, null)).toBe(false);
        expect(facade.isPromptVisible({ ...activeSession, endedAtUtc: new Date().toISOString() }, prompt)).toBe(false);
        facade.dismissPrompt(prompt.id);
        facade.snoozePrompt(prompt.id);
        expect(facade.promptState()).toEqual({});
    });

    it('dismisses only the current session prompt and persists its state', () => {
        facade.currentSession.set(activeSession);
        expect(facade.isPromptVisible(activeSession, prompt)).toBe(true);
        facade.dismissPrompt(prompt.id);
        expect(facade.isPromptVisible(activeSession, prompt)).toBe(false);
        expect(facade.isPromptVisible({ ...activeSession, id: 'another' }, prompt)).toBe(true);
        expect(Object.values(facade.promptState())).toEqual([{ dismissed: true }]);
    });

    it('shows a snoozed prompt again only after the saved deadline', () => {
        facade.currentSession.set(activeSession);
        facade.snoozePrompt(prompt.id);
        expect(facade.isPromptVisible(activeSession, prompt)).toBe(false);
        const state = Object.values(facade.promptState())[0];
        const deadline = Date.parse(state?.snoozedUntilUtc ?? '');
        expect(Number.isFinite(deadline)).toBe(true);
        facade.now.set(new Date(deadline - 1));
        expect(facade.isPromptVisible(activeSession, prompt)).toBe(false);
        facade.now.set(new Date(deadline));
        expect(facade.isPromptVisible(activeSession, prompt)).toBe(true);
    });
});

type FastingServiceMock = {
    getOverview: ReturnType<typeof vi.fn>;
    getOverviewStrict: ReturnType<typeof vi.fn>;
    getHistory: ReturnType<typeof vi.fn>;
    updateCheckIn: ReturnType<typeof vi.fn>;
    end: ReturnType<typeof vi.fn>;
    start: ReturnType<typeof vi.fn>;
    extend: ReturnType<typeof vi.fn>;
    reduceTarget: ReturnType<typeof vi.fn>;
    skipCyclicDay: ReturnType<typeof vi.fn>;
    postponeCyclicDay: ReturnType<typeof vi.fn>;
};

function setupFacade(): void {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-04-13T09:45:00Z'));

    activeSession = createActiveSession();
    baseOverview = createBaseOverview();
    fastingService = createFastingServiceMock(baseOverview);
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
}

function teardownFacade(): void {
    vi.clearAllTimers();
    vi.useRealTimers();
}

function createFastingServiceMock(overview: FastingOverview): FastingServiceMock {
    return {
        getOverview: vi.fn().mockReturnValue(of(overview)),
        getOverviewStrict: vi.fn().mockReturnValue(of(overview)),
        getHistory: vi
            .fn()
            .mockReturnValue(of({ data: [], page: HISTORY_PAGE, limit: 10, totalPages: HISTORY_PAGE, totalItems: HISTORY_TOTAL_ITEMS })),
        updateCheckIn: vi.fn(),
        end: vi.fn(),
        start: vi.fn(),
        extend: vi.fn(),
        reduceTarget: vi.fn(),
        skipCyclicDay: vi.fn(),
        postponeCyclicDay: vi.fn(),
    };
}

function createActiveSession(): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-04-12T06:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: DEFAULT_FASTING_HOURS,
        addedDurationHours: 0,
        plannedDurationHours: DEFAULT_FASTING_HOURS,
        protocol: 'Fast16Eat8',
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
}

function createBaseOverview(): FastingOverview {
    return {
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
}
