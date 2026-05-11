import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../services/localization.service';
import { FastingFacade } from '../../lib/fasting.facade';
import type { FastingProtocol, FastingSession } from '../../models/fasting.data';
import { FastingControlsComponent } from './fasting-controls.component';

const CUSTOM_EXTEND_HOURS = 12;
const CUSTOM_REDUCE_HOURS = 2;
const WARNING_PLANNED_DURATION_HOURS = 80;
const HARD_STOP_PLANNED_DURATION_HOURS = 160;
const HARD_STOP_EXTEND_HOURS = 24;
const INTERMITTENT_FAST_HOURS = 16;
const DEFAULT_EXTEND_HOURS = 24;
const DEFAULT_REDUCE_HOURS = 4;
const DEFAULT_EATING_WINDOW_HOURS = 8;

let fixture: ComponentFixture<FastingControlsComponent>;
let component: FastingControlsComponent;
let facade: FastingFacadeMock;
let dialogService: { open: ReturnType<typeof vi.fn> };

beforeEach(async () => {
    facade = createFacadeMock();
    dialogService = {
        open: vi.fn(() => ({
            afterClosed: (): Observable<string> => of('confirm'),
        })),
    };

    await TestBed.configureTestingModule({
        imports: [FastingControlsComponent, TranslateModule.forRoot()],
        providers: [
            { provide: FastingFacade, useValue: facade },
            { provide: FdUiDialogService, useValue: dialogService },
            {
                provide: LocalizationService,
                useValue: {
                    getCurrentLanguage: vi.fn(() => 'ru'),
                },
            },
        ],
    }).compileComponents();

    fixture = TestBed.createComponent(FastingControlsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
});

describe('FastingControlsComponent setup controls', () => {
    it('renders setup controls and starts fasting from the CTA', () => {
        expect(host(fixture).textContent).toContain('FASTING.MODE_INTERMITTENT');
        expect(host(fixture).textContent).toContain('FASTING.START_FAST');

        const startButton = getButtonByText(fixture, 'FASTING.START_FAST');
        startButton.click();

        expect(facade.startFasting).toHaveBeenCalledTimes(1);
    });

    it('delegates mode and protocol changes to the shared facade', () => {
        component.onModeChange('extended');
        component.onProtocolChange('F36');
        component.onCyclicPresetChange('2:1');

        expect(facade.selectMode).toHaveBeenCalledWith('extended');
        expect(facade.selectProtocol).toHaveBeenCalledWith('F36');
        expect(facade.setCyclicPreset).toHaveBeenCalledWith(2, 1);
    });

    it('ignores invalid numeric input values', () => {
        component.onCustomHoursChange('abc');
        component.onCustomIntermittentFastHoursChange('abc');
        component.onCyclicFastDaysChange('abc');
        component.onCyclicEatDaysChange('abc');
        component.onCyclicEatDayFastHoursChange('abc');
        component.onExtendHoursChange('abc');
        component.onReduceHoursChange('abc');

        expect(facade.setCustomHours).not.toHaveBeenCalled();
        expect(facade.setCustomIntermittentFastHours).not.toHaveBeenCalled();
        expect(facade.setCyclicFastDays).not.toHaveBeenCalled();
        expect(facade.setCyclicEatDays).not.toHaveBeenCalled();
        expect(facade.setCyclicEatDayFastHours).not.toHaveBeenCalled();
        expect(facade.setExtendHours).not.toHaveBeenCalled();
        expect(facade.setReduceHours).not.toHaveBeenCalled();
    });
});

describe('FastingControlsComponent active cyclic session', () => {
    it('renders active cyclic controls without setup start CTA', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createCyclicSession());
        fixture.detectChanges();

        const text = host(fixture).textContent;
        expect(text).toContain('FASTING.SKIP_FASTING_PERIOD');
        expect(text).toContain('FASTING.SKIP_DAY');
        expect(text).toContain('FASTING.STOP_CYCLE');
        expect(text).not.toContain('FASTING.START_FAST');
    });

    it('confirms before ending the active session', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createCyclicSession());
        fixture.detectChanges();

        component.endFasting();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.endFasting).toHaveBeenCalledTimes(1);
    });

    it('confirms cyclic day management actions', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createCyclicSession());
        fixture.detectChanges();

        component.skipCyclicDay();
        component.postponeCyclicDay();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(facade.skipCyclicDay).toHaveBeenCalledTimes(1);
        expect(facade.postponeCyclicDay).toHaveBeenCalledTimes(1);
    });
});

describe('FastingControlsComponent active extended session', () => {
    it('keeps extended adjustment panels collapsed by default', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createExtendedSession());
        fixture.detectChanges();

        const text = host(fixture).textContent;
        expect(text).toContain('FASTING.EXTEND_GROUP');
        expect(text).toContain('FASTING.REDUCE_GROUP');
        expect(text).not.toContain('FASTING.ADD_DAY');
        expect(text).not.toContain('FASTING.REDUCE_4_HOURS');
        expect(text).not.toContain('FASTING.START_FAST');
    });

    it('expands extended controls and delegates custom duration actions', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createExtendedSession());
        facade.extendHours.set(CUSTOM_EXTEND_HOURS);
        facade.reduceHours.set(CUSTOM_REDUCE_HOURS);
        fixture.detectChanges();

        component.showCustomExtend();
        component.showCustomReduce();
        fixture.detectChanges();

        const text = host(fixture).textContent;
        expect(text).toContain('FASTING.ADD_TIME');
        expect(text).toContain('FASTING.REDUCE_TIME');

        component.extendByCustom();
        component.reduceByCustom();

        expect(facade.extendByHours).toHaveBeenCalledWith(CUSTOM_EXTEND_HOURS);
        expect(facade.reduceTargetByHours).toHaveBeenCalledWith(CUSTOM_REDUCE_HOURS);
    });

    it('asks for safety confirmation before extending beyond the warning threshold', () => {
        facade.isActive.set(true);
        facade.currentSession.set({ ...createExtendedSession(), plannedDurationHours: WARNING_PLANNED_DURATION_HOURS });
        facade.extendHours.set(CUSTOM_EXTEND_HOURS);
        fixture.detectChanges();

        component.extendByCustom();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.extendByHours).toHaveBeenCalledWith(CUSTOM_EXTEND_HOURS);
    });

    it('blocks extensions beyond the hard stop threshold', () => {
        facade.isActive.set(true);
        facade.currentSession.set({ ...createExtendedSession(), plannedDurationHours: HARD_STOP_PLANNED_DURATION_HOURS });
        fixture.detectChanges();

        component.onExtendHoursChange(HARD_STOP_EXTEND_HOURS);
        component.extendByCustom();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.extendByHours).not.toHaveBeenCalled();
    });
});

function host(componentFixture: ComponentFixture<FastingControlsComponent>): HTMLElement {
    return componentFixture.nativeElement as HTMLElement;
}

function getButtonByText(componentFixture: ComponentFixture<FastingControlsComponent>, text: string): HTMLElement {
    const button = Array.from(host(componentFixture).querySelectorAll<HTMLElement>('fd-ui-button')).find(element =>
        element.textContent.includes(text),
    );
    if (button === undefined) {
        throw new Error(`Button with text "${text}" was not found.`);
    }

    return button;
}

type FastingFacadeMock = {
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
    extendByHours: ReturnType<typeof vi.fn>;
    reduceTargetByHours: ReturnType<typeof vi.fn>;
    skipCyclicDay: ReturnType<typeof vi.fn>;
    postponeCyclicDay: ReturnType<typeof vi.fn>;
    isActive: WritableSignal<boolean>;
    currentSession: WritableSignal<FastingSession | null>;
    selectedMode: WritableSignal<'intermittent' | 'extended' | 'cyclic'>;
    selectedProtocol: WritableSignal<FastingProtocol>;
    customHours: WritableSignal<number>;
    customIntermittentFastHours: WritableSignal<number>;
    cyclicEatDayProtocol: WritableSignal<FastingProtocol>;
    cyclicFastDays: WritableSignal<number>;
    cyclicEatDays: WritableSignal<number>;
    cyclicUsesCustomPreset: WritableSignal<boolean>;
    cyclicEatDayFastHours: WritableSignal<number>;
    extendHours: WritableSignal<number>;
    reduceHours: WritableSignal<number>;
    isStarting: WritableSignal<boolean>;
    isEnding: WritableSignal<boolean>;
    isExtending: WritableSignal<boolean>;
    isReducing: WritableSignal<boolean>;
    isUpdatingCycle: WritableSignal<boolean>;
    canExtendActiveSession: WritableSignal<boolean>;
};

function createFacadeMock(): FastingFacadeMock {
    return {
        ...createFacadeActionMocks(),
        ...createFacadeSignals(),
    };
}

function createFacadeActionMocks(): Pick<
    FastingFacadeMock,
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
    | 'extendByHours'
    | 'reduceTargetByHours'
    | 'skipCyclicDay'
    | 'postponeCyclicDay'
> {
    return {
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
        extendByHours: vi.fn(),
        reduceTargetByHours: vi.fn(),
        skipCyclicDay: vi.fn(),
        postponeCyclicDay: vi.fn(),
    };
}

function createFacadeSignals(): Omit<FastingFacadeMock, keyof ReturnType<typeof createFacadeActionMocks>> {
    return {
        isActive: signal(false),
        currentSession: signal<FastingSession | null>(null),
        selectedMode: signal<'intermittent' | 'extended' | 'cyclic'>('intermittent'),
        selectedProtocol: signal<FastingProtocol>('F16_8'),
        customHours: signal(INTERMITTENT_FAST_HOURS),
        customIntermittentFastHours: signal(INTERMITTENT_FAST_HOURS),
        cyclicEatDayProtocol: signal<FastingProtocol>('F16_8'),
        cyclicFastDays: signal(1),
        cyclicEatDays: signal(1),
        cyclicUsesCustomPreset: signal(false),
        cyclicEatDayFastHours: signal(INTERMITTENT_FAST_HOURS),
        extendHours: signal(DEFAULT_EXTEND_HOURS),
        reduceHours: signal(DEFAULT_REDUCE_HOURS),
        isStarting: signal(false),
        isEnding: signal(false),
        isExtending: signal(false),
        isReducing: signal(false),
        isUpdatingCycle: signal(false),
        canExtendActiveSession: signal(true),
    };
}

function createCyclicSession(): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-04-12T06:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: DEFAULT_EXTEND_HOURS,
        addedDurationHours: 0,
        plannedDurationHours: DEFAULT_EXTEND_HOURS,
        protocol: 'Cyclic',
        planType: 'Cyclic',
        occurrenceKind: 'FastDay',
        cyclicFastDays: 1,
        cyclicEatDays: 1,
        cyclicEatDayFastHours: INTERMITTENT_FAST_HOURS,
        cyclicEatDayEatingWindowHours: DEFAULT_EATING_WINDOW_HOURS,
        cyclicPhaseDayNumber: 1,
        cyclicPhaseDayTotal: 1,
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

function createExtendedSession(): FastingSession {
    return {
        ...createCyclicSession(),
        id: 'extended-session-1',
        protocol: 'F24',
        planType: 'Extended',
        occurrenceKind: 'FastDay',
        cyclicFastDays: null,
        cyclicEatDays: null,
        cyclicEatDayFastHours: null,
        cyclicEatDayEatingWindowHours: null,
        cyclicPhaseDayNumber: null,
        cyclicPhaseDayTotal: null,
    };
}
