import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../services/localization.service';
import type { FastingCheckInChartDialogData } from '../../components/fasting-checkin-chart-dialog/fasting-checkin-chart-dialog.component';
import { FastingFacade } from '../../lib/fasting.facade';
import type { FastingInsights, FastingProtocol, FastingSession, FastingStats } from '../../models/fasting.data';
import { FastingPageComponent } from './fasting-page.component';

const SESSION_CHECK_INS_BEYOND_PAGE_SIZE = 6;
const DEFAULT_SESSION_HOURS = 16;
const DEFAULT_EXTEND_HOURS = 24;
const DEFAULT_REDUCE_HOURS = 4;
const CHECK_IN_HUNGER_LEVEL = 2;
const CHECK_IN_ENERGY_LEVEL = 4;
const CHECK_IN_MOOD_LEVEL = 3;
const HISTORY_START_HOUR = 10;
const INITIAL_VISIBLE_CHECK_INS = 5;

let component: FastingPageComponent;
let fixture: ComponentFixture<FastingPageComponent>;
let facade: FastingFacadeMock;
let toastService: { success: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };

describe('FastingPageComponent setup and actions', () => {
    beforeEach(createComponentAsync);

    it('initializes facade on init', () => {
        expect(facade.initialize).toHaveBeenCalledTimes(1);
    });

    it('builds current check-in timeline when current check-in exists', () => {
        facade.currentSession.set({
            ...createSession(),
            checkIns: [
                {
                    id: 'checkin-1',
                    checkedInAtUtc: '2026-04-12T10:00:00Z',
                    hungerLevel: 2,
                    energyLevel: 4,
                    moodLevel: 4,
                    symptoms: ['weakness'],
                    notes: 'steady',
                },
            ],
        });

        expect(component.hasCurrentSessionTimeline()).toBe(true);
        expect(component.currentSessionRecentCheckIns()).toHaveLength(1);
    });

    it('opens and closes check-in form while resetting draft on close', () => {
        component.openCheckInForm();
        expect(component.isCheckInExpanded()).toBe(true);

        component.closeCheckInForm();

        expect(component.isCheckInExpanded()).toBe(false);
        expect(facade.resetCheckInDraft).toHaveBeenCalledTimes(1);
    });

    it('delegates save and load more actions to facade', () => {
        component.saveCheckIn();
        component.loadMoreHistory();

        expect(facade.saveCheckIn).toHaveBeenCalledTimes(1);
        expect(facade.loadMoreHistory).toHaveBeenCalledTimes(1);
    });

    it('shows success toast and collapses check-in after saved version changes', () => {
        component.openCheckInForm();
        facade.checkInSavedVersion.set(1);
        fixture.detectChanges();

        expect(component.isCheckInExpanded()).toBe(false);
        expect(toastService.success).toHaveBeenCalledWith('FASTING.CHECK_IN.SAVED_TOAST');
    });

    it('toggles history accordion with single expanded session', () => {
        component.toggleHistorySession('session-1');
        expect(component.isHistorySessionExpanded('session-1')).toBe(true);
        expect(component.getHistoryCheckInToggleKey(createHistorySession('session-1', 2))).toBe('FASTING.HIDE_HISTORY_CHECK_INS');

        component.toggleHistorySession('session-2');
        expect(component.isHistorySessionExpanded('session-1')).toBe(false);
        expect(component.isHistorySessionExpanded('session-2')).toBe(true);

        component.toggleHistorySession('session-2');
        expect(component.isHistorySessionExpanded('session-2')).toBe(false);
        expect(component.getHistoryCheckInToggleKey(createHistorySession('session-2', 2))).toBe('FASTING.SHOW_HISTORY_CHECK_INS');
    });
});

describe('FastingPageComponent check-in history', () => {
    beforeEach(createComponentAsync);

    it('opens chart dialog only for sessions with multiple check-ins', () => {
        const singleCheckInSession = createHistorySession('session-1', 1);
        const multiCheckInSession = createHistorySession('session-2', 2);

        component.openSessionCheckInChart(singleCheckInSession);
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(component.canViewSessionCheckInChart(singleCheckInSession)).toBe(false);

        component.openSessionCheckInChart(multiCheckInSession);

        expect(component.canViewSessionCheckInChart(multiCheckInSession)).toBe(true);
        expect(dialogService.open).toHaveBeenCalledTimes(1);
        const call = dialogService.open.mock.calls[0] as
            | [unknown, { size?: string; panelClass?: string; data?: FastingCheckInChartDialogData }]
            | undefined;
        const options = call?.[1];

        expect(options?.size).toBe('lg');
        expect(options?.panelClass).toBe('fd-ui-dialog-panel--chart');
        expect(options?.data?.title).toBe('FASTING.CHECK_IN.CHART_TITLE');
        expect(options?.data?.checkIns).toEqual(multiCheckInSession.checkIns);
    });

    it('tracks session check-in pagination locally', () => {
        const session = createHistorySession('session-3', SESSION_CHECK_INS_BEYOND_PAGE_SIZE);

        expect(component.getVisibleSessionCheckIns(session)).toHaveLength(INITIAL_VISIBLE_CHECK_INS);
        expect(component.canLoadMoreSessionCheckIns(session)).toBe(true);

        component.loadMoreSessionCheckIns(session.id);

        expect(component.getVisibleSessionCheckIns(session)).toHaveLength(SESSION_CHECK_INS_BEYOND_PAGE_SIZE);
        expect(component.canLoadMoreSessionCheckIns(session)).toBe(false);
    });

    it('treats sessions without check-ins as collapsed non-chartable history items', () => {
        const session = createHistorySession('session-4', 0);

        expect(component.hasCheckIn(session)).toBe(false);
        expect(component.getSessionCheckInCount(session)).toBe(0);
        expect(component.canViewSessionCheckInChart(session)).toBe(false);
        expect(component.getVisibleSessionCheckIns(session)).toHaveLength(0);
    });

    it('does not allow chart for single legacy summary check-in fallback', () => {
        const session = {
            ...createHistorySession('session-5', 0),
            checkInAtUtc: '2026-04-12T10:00:00Z',
            hungerLevel: CHECK_IN_HUNGER_LEVEL,
            energyLevel: CHECK_IN_ENERGY_LEVEL,
            moodLevel: CHECK_IN_ENERGY_LEVEL,
            symptoms: ['weakness'],
            checkInNotes: 'legacy',
        };

        expect(component.hasCheckIn(session)).toBe(true);
        expect(component.getSessionCheckInCount(session)).toBe(1);
        expect(component.canViewSessionCheckInChart(session)).toBe(false);
        expect(component.getVisibleSessionCheckIns(session)).toHaveLength(1);
    });
});

async function createComponentAsync(): Promise<void> {
    facade = createFacadeMock();
    toastService = {
        success: vi.fn(),
    };
    dialogService = {
        open: vi.fn((): { afterClosed: () => Observable<undefined> } => ({ afterClosed: () => of(undefined) })),
    };

    await TestBed.configureTestingModule({
        imports: [FastingPageComponent],
        providers: [
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                },
            },
            {
                provide: FdUiDialogService,
                useValue: dialogService,
            },
            {
                provide: LocalizationService,
                useValue: {
                    getCurrentLanguage: vi.fn(() => 'ru'),
                },
            },
            { provide: FdUiToastService, useValue: toastService },
        ],
    })
        .overrideComponent(FastingPageComponent, {
            set: {
                template: '<div></div>',
                providers: [{ provide: FastingFacade, useValue: facade }],
            },
        })
        .compileComponents();

    fixture = TestBed.createComponent(FastingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

type FastingFacadeMock = {
    initialize: ReturnType<typeof vi.fn>;
    loadMoreHistory: ReturnType<typeof vi.fn>;
    startFasting: ReturnType<typeof vi.fn>;
    endFasting: ReturnType<typeof vi.fn>;
    selectMode: ReturnType<typeof vi.fn>;
    selectProtocol: ReturnType<typeof vi.fn>;
    setCustomHours: ReturnType<typeof vi.fn>;
    setCustomIntermittentFastHours: ReturnType<typeof vi.fn>;
    setCyclicPreset: ReturnType<typeof vi.fn>;
    selectCustomCyclicPreset: ReturnType<typeof vi.fn>;
    setCyclicFastDays: ReturnType<typeof vi.fn>;
    setCyclicEatDays: ReturnType<typeof vi.fn>;
    selectCyclicEatDayProtocol: ReturnType<typeof vi.fn>;
    setCyclicEatDayFastHours: ReturnType<typeof vi.fn>;
    setExtendHours: ReturnType<typeof vi.fn>;
    setReduceHours: ReturnType<typeof vi.fn>;
    setHungerLevel: ReturnType<typeof vi.fn>;
    setEnergyLevel: ReturnType<typeof vi.fn>;
    setMoodLevel: ReturnType<typeof vi.fn>;
    toggleSymptom: ReturnType<typeof vi.fn>;
    setCheckInNotes: ReturnType<typeof vi.fn>;
    saveCheckIn: ReturnType<typeof vi.fn>;
    resetCheckInDraft: ReturnType<typeof vi.fn>;
    dismissPrompt: ReturnType<typeof vi.fn>;
    snoozePrompt: ReturnType<typeof vi.fn>;
    extendByHours: ReturnType<typeof vi.fn>;
    reduceTargetByHours: ReturnType<typeof vi.fn>;
    skipCyclicDay: ReturnType<typeof vi.fn>;
    postponeCyclicDay: ReturnType<typeof vi.fn>;
    isPromptVisible: ReturnType<typeof vi.fn>;
    isLoading: ReturnType<typeof signal<boolean>>;
    isStarting: ReturnType<typeof signal<boolean>>;
    isEnding: ReturnType<typeof signal<boolean>>;
    isExtending: ReturnType<typeof signal<boolean>>;
    isReducing: ReturnType<typeof signal<boolean>>;
    isUpdatingCycle: ReturnType<typeof signal<boolean>>;
    isSavingCheckIn: ReturnType<typeof signal<boolean>>;
    currentSession: WritableSignal<FastingSession | null>;
    stats: WritableSignal<FastingStats | null>;
    history: WritableSignal<FastingSession[]>;
    historyPage: ReturnType<typeof signal<number>>;
    historyTotalPages: ReturnType<typeof signal<number>>;
    isLoadingMoreHistory: ReturnType<typeof signal<boolean>>;
    insightsData: WritableSignal<FastingInsights>;
    checkInSavedVersion: ReturnType<typeof signal<number>>;
    selectedMode: ReturnType<typeof signal<'intermittent' | 'extended' | 'cyclic'>>;
    selectedProtocol: WritableSignal<FastingProtocol>;
    customHours: ReturnType<typeof signal<number>>;
    customIntermittentFastHours: ReturnType<typeof signal<number>>;
    cyclicEatDayProtocol: WritableSignal<FastingProtocol>;
    cyclicFastDays: ReturnType<typeof signal<number>>;
    cyclicEatDays: ReturnType<typeof signal<number>>;
    cyclicUsesCustomPreset: ReturnType<typeof signal<boolean>>;
    cyclicEatDayFastHours: ReturnType<typeof signal<number>>;
    extendHours: ReturnType<typeof signal<number>>;
    reduceHours: ReturnType<typeof signal<number>>;
    hungerLevel: ReturnType<typeof signal<number>>;
    energyLevel: ReturnType<typeof signal<number>>;
    moodLevel: ReturnType<typeof signal<number>>;
    selectedSymptoms: ReturnType<typeof signal<string[]>>;
    checkInNotes: ReturnType<typeof signal<string>>;
    progressPercent: ReturnType<typeof signal<number>>;
    elapsedFormatted: ReturnType<typeof signal<string>>;
    remainingFormatted: ReturnType<typeof signal<string>>;
    isOvertime: ReturnType<typeof signal<boolean>>;
    isActive: ReturnType<typeof signal<boolean>>;
    canExtendActiveSession: ReturnType<typeof signal<boolean>>;
};

function createFacadeMock(): FastingFacadeMock {
    return {
        ...createFacadeActions(),
        ...createFacadeState(),
    };
}

function createFacadeActions(): Pick<
    FastingFacadeMock,
    | 'initialize'
    | 'loadMoreHistory'
    | 'startFasting'
    | 'endFasting'
    | 'selectMode'
    | 'selectProtocol'
    | 'setCustomHours'
    | 'setCustomIntermittentFastHours'
    | 'setCyclicPreset'
    | 'selectCustomCyclicPreset'
    | 'setCyclicFastDays'
    | 'setCyclicEatDays'
    | 'selectCyclicEatDayProtocol'
    | 'setCyclicEatDayFastHours'
    | 'setExtendHours'
    | 'setReduceHours'
    | 'setHungerLevel'
    | 'setEnergyLevel'
    | 'setMoodLevel'
    | 'toggleSymptom'
    | 'setCheckInNotes'
    | 'saveCheckIn'
    | 'resetCheckInDraft'
    | 'dismissPrompt'
    | 'snoozePrompt'
    | 'extendByHours'
    | 'reduceTargetByHours'
    | 'skipCyclicDay'
    | 'postponeCyclicDay'
    | 'isPromptVisible'
> {
    return {
        initialize: vi.fn(),
        loadMoreHistory: vi.fn(),
        startFasting: vi.fn(),
        endFasting: vi.fn(),
        selectMode: vi.fn(),
        selectProtocol: vi.fn(),
        setCustomHours: vi.fn(),
        setCustomIntermittentFastHours: vi.fn(),
        setCyclicPreset: vi.fn(),
        selectCustomCyclicPreset: vi.fn(),
        setCyclicFastDays: vi.fn(),
        setCyclicEatDays: vi.fn(),
        selectCyclicEatDayProtocol: vi.fn(),
        setCyclicEatDayFastHours: vi.fn(),
        setExtendHours: vi.fn(),
        setReduceHours: vi.fn(),
        setHungerLevel: vi.fn(),
        setEnergyLevel: vi.fn(),
        setMoodLevel: vi.fn(),
        toggleSymptom: vi.fn(),
        setCheckInNotes: vi.fn(),
        saveCheckIn: vi.fn(),
        resetCheckInDraft: vi.fn(),
        dismissPrompt: vi.fn(),
        snoozePrompt: vi.fn(),
        extendByHours: vi.fn(),
        reduceTargetByHours: vi.fn(),
        skipCyclicDay: vi.fn(),
        postponeCyclicDay: vi.fn(),
        isPromptVisible: vi.fn(() => true),
    };
}

function createFacadeState(): Omit<FastingFacadeMock, keyof ReturnType<typeof createFacadeActions>> {
    return {
        isLoading: signal(false),
        isStarting: signal(false),
        isEnding: signal(false),
        isExtending: signal(false),
        isReducing: signal(false),
        isUpdatingCycle: signal(false),
        isSavingCheckIn: signal(false),
        currentSession: signal<FastingSession | null>(null),
        stats: signal<FastingStats | null>(null),
        history: signal<FastingSession[]>([]),
        historyPage: signal(1),
        historyTotalPages: signal(1),
        isLoadingMoreHistory: signal(false),
        insightsData: signal<FastingInsights>({ alerts: [], insights: [] }),
        checkInSavedVersion: signal(0),
        selectedMode: signal<'intermittent' | 'extended' | 'cyclic'>('intermittent'),
        selectedProtocol: signal<FastingProtocol>('F16_8'),
        customHours: signal(DEFAULT_SESSION_HOURS),
        customIntermittentFastHours: signal(DEFAULT_SESSION_HOURS),
        cyclicEatDayProtocol: signal<FastingProtocol>('F16_8'),
        cyclicFastDays: signal(1),
        cyclicEatDays: signal(1),
        cyclicUsesCustomPreset: signal(false),
        cyclicEatDayFastHours: signal(DEFAULT_SESSION_HOURS),
        extendHours: signal(DEFAULT_EXTEND_HOURS),
        reduceHours: signal(DEFAULT_REDUCE_HOURS),
        hungerLevel: signal(CHECK_IN_MOOD_LEVEL),
        energyLevel: signal(CHECK_IN_MOOD_LEVEL),
        moodLevel: signal(CHECK_IN_MOOD_LEVEL),
        selectedSymptoms: signal<string[]>([]),
        checkInNotes: signal(''),
        progressPercent: signal(0),
        elapsedFormatted: signal('00:00:00'),
        remainingFormatted: signal('00:00:00'),
        isOvertime: signal(false),
        isActive: signal(false),
        canExtendActiveSession: signal(false),
    };
}

function createSession(): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-04-12T06:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: DEFAULT_SESSION_HOURS,
        addedDurationHours: 0,
        plannedDurationHours: DEFAULT_SESSION_HOURS,
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
}

function createHistorySession(id: string, checkInCount: number): FastingSession {
    const checkIns = Array.from({ length: checkInCount }, (_, index) => ({
        id: `${id}-checkin-${index + 1}`,
        checkedInAtUtc: `2026-04-12T${String(HISTORY_START_HOUR + index).padStart(2, '0')}:00:00Z`,
        hungerLevel: CHECK_IN_HUNGER_LEVEL,
        energyLevel: CHECK_IN_ENERGY_LEVEL,
        moodLevel: CHECK_IN_ENERGY_LEVEL,
        symptoms: index === 0 ? ['weakness'] : [],
        notes: index === 0 ? 'steady' : null,
    }));

    return {
        ...createSession(),
        id,
        checkIns,
        ...createLatestCheckInSessionFields(checkIns[0]),
    };
}

function createLatestCheckInSessionFields(checkIn: FastingSession['checkIns'][number] | undefined): Partial<FastingSession> {
    if (checkIn === undefined) {
        return {
            checkInAtUtc: null,
            hungerLevel: null,
            energyLevel: null,
            moodLevel: null,
            symptoms: [],
            checkInNotes: null,
        };
    }

    return {
        checkInAtUtc: checkIn.checkedInAtUtc,
        hungerLevel: checkIn.hungerLevel,
        energyLevel: checkIn.energyLevel,
        moodLevel: checkIn.moodLevel,
        symptoms: checkIn.symptoms,
        checkInNotes: checkIn.notes,
    };
}
