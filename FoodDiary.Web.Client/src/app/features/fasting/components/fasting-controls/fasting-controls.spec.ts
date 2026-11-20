import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import type { FastingMode, FastingProtocol, FastingSession } from '../../../../shared/models/fasting.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { FastingFacade } from '../../lib/fasting.facade';
import { FastingEndConfirmDialogComponent } from '../fasting-end-confirm-dialog/fasting-end-confirm-dialog';
import { FastingSafetyDialogComponent } from '../fasting-safety-dialog/fasting-safety-dialog';
import { FastingControlsComponent } from './fasting-controls';

const CUSTOM_EXTEND_HOURS = 12;
const CUSTOM_REDUCE_HOURS = 2;
const WARNING_PLANNED_DURATION_HOURS = 80;
const HARD_STOP_PLANNED_DURATION_HOURS = 160;
const HARD_STOP_EXTEND_HOURS = 24;
const INTERMITTENT_FAST_HOURS = 16;
const DEFAULT_EXTEND_HOURS = 24;
const DEFAULT_REDUCE_HOURS = 4;
const DEFAULT_EATING_WINDOW_HOURS = 8;
const SHORT_REDUCIBLE_HOURS = 2;
const CUSTOM_CYCLIC_FAST_DAYS = 3;
const CUSTOM_CYCLIC_EAT_DAYS = 2;
const CUSTOM_EAT_DAY_FAST_HOURS = 18;
const CUSTOM_EXTENDED_FAST_HOURS = 48;

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
        imports: [FastingControlsComponent],
        providers: [
            provideTranslateTesting(),
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

    it('fits the mode selector to the available width', () => {
        const toggles = fixture.debugElement.queryAll(By.directive(FdUiSegmentedToggleComponent));
        const modeToggle = toggles[0].componentInstance as FdUiSegmentedToggleComponent;

        expect(modeToggle.fullWidth()).toBe(true);
        expect(modeToggle.shrinkItems()).toBe(false);
        expect(modeToggle.stackOnNarrow()).toBe(true);
    });

    it('delegates mode and protocol changes to the shared facade', () => {
        component['onModeChange']('extended');
        component['onProtocolChange']('Fast36');
        component['onCyclicPresetChange']('2:1');

        expect(facade.selectMode).toHaveBeenCalledWith('extended');
        expect(facade.selectProtocol).toHaveBeenCalledWith('Fast36');
        expect(facade.setCyclicPreset).toHaveBeenCalledWith(2, 1);
    });

    it('ignores invalid numeric input values', () => {
        component['onCustomHoursChange']('abc');
        component['onCustomIntermittentFastHoursChange']('abc');
        component['onCyclicFastDaysChange']('abc');
        component['onCyclicEatDaysChange']('abc');
        component['onCyclicEatDayFastHoursChange']('abc');
        component['onExtendHoursChange']('abc');
        component['onReduceHoursChange']('abc');

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

        component['endFasting']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.endFasting).toHaveBeenCalledTimes(1);
    });

    it('confirms cyclic day management actions', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createCyclicSession());
        fixture.detectChanges();

        component['skipCyclicDay']();
        component['postponeCyclicDay']();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(facade.skipCyclicDay).toHaveBeenCalledTimes(1);
        expect(facade.postponeCyclicDay).toHaveBeenCalledTimes(1);
    });
});

describe('FastingControlsComponent rendered configuration', () => {
    it('edits a custom intermittent window and a custom extended target', () => {
        facade.selectMode.mockImplementation((mode: FastingMode) => {
            facade.selectedMode.set(mode);
        });
        facade.selectProtocol.mockImplementation((protocol: FastingProtocol) => {
            facade.selectedProtocol.set(protocol);
        });
        facade.setCustomIntermittentFastHours.mockImplementation((hours: number) => {
            facade.customIntermittentFastHours.set(hours);
        });

        clickRadio('FASTING.INTERMITTENT_GROUP', 'FASTING.PROTOCOL_CUSTOM_INTERMITTENT');
        enterNumber('FASTING.FAST_HOURS_SHORT', CUSTOM_EAT_DAY_FAST_HOURS);
        expect(facade.setCustomIntermittentFastHours).toHaveBeenCalledWith(CUSTOM_EAT_DAY_FAST_HOURS);
        expect(host(fixture).querySelector('.fasting-controls__custom-intermittent-summary')?.textContent.trim()).toBe('18:6');

        clickRadio('FASTING.MODE_GROUP', 'FASTING.MODE_EXTENDED');
        clickRadio('FASTING.EXTENDED_GROUP', 'FASTING.PROTOCOL_CUSTOM');
        enterNumber('FASTING.CUSTOM_HOURS', CUSTOM_EXTENDED_FAST_HOURS);
        expect(facade.selectMode).toHaveBeenCalledWith('extended');
        expect(facade.selectProtocol).toHaveBeenLastCalledWith('Custom');
        expect(facade.setCustomHours).toHaveBeenCalledWith(CUSTOM_EXTENDED_FAST_HOURS);
    });

    it('offers a custom cycle and custom eating-day window inside the settings dialog', () => {
        fixture.componentRef.setInput('presentation', 'dialog');
        fixture.componentRef.setInput('showSetupStartAction', false);
        facade.selectMode.mockImplementation((mode: FastingMode) => {
            facade.selectedMode.set(mode);
        });
        facade.selectCustomCyclicPreset.mockImplementation(() => {
            facade.cyclicUsesCustomPreset.set(true);
        });
        facade.selectCyclicEatDayProtocol.mockImplementation((protocol: FastingProtocol) => {
            facade.cyclicEatDayProtocol.set(protocol);
        });
        fixture.detectChanges();

        clickRadio('FASTING.MODE_GROUP', 'FASTING.MODE_CYCLIC');
        clickRadio('FASTING.CYCLIC_GROUP', 'FASTING.CUSTOM_CYCLE');
        enterNumber('FASTING.CYCLIC_FAST_DAYS_SHORT', CUSTOM_CYCLIC_FAST_DAYS);
        enterNumber('FASTING.CYCLIC_EAT_DAYS_SHORT', CUSTOM_CYCLIC_EAT_DAYS);
        clickRadio('FASTING.EAT_DAY_WINDOW_GROUP', 'FASTING.PROTOCOL_CUSTOM_INTERMITTENT');
        enterNumber('FASTING.FAST_HOURS_SHORT', CUSTOM_EAT_DAY_FAST_HOURS);

        expect(facade.setCyclicFastDays).toHaveBeenCalledWith(CUSTOM_CYCLIC_FAST_DAYS);
        expect(facade.setCyclicEatDays).toHaveBeenCalledWith(CUSTOM_CYCLIC_EAT_DAYS);
        expect(facade.setCyclicEatDayFastHours).toHaveBeenCalledWith(CUSTOM_EAT_DAY_FAST_HOURS);
        expect(host(fixture).textContent).toContain('FASTING.REDESIGN.CYCLIC_SELECTION_SUMMARY');
        expect(host(fixture).textContent).not.toContain('FASTING.START_FAST');
    });
});

describe('FastingControlsComponent rendered active actions', () => {
    it('asks to start fasting early or skip an eating day, and applies only the confirmed action', () => {
        facade.isActive.set(true);
        facade.currentSession.set({ ...createCyclicSession(), occurrenceKind: 'EatDay' });
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of('cancel') });
        fixture.detectChanges();

        getNativeButtonByText(fixture, 'FASTING.START_FAST_NOW').click();
        expect(dialogService.open.mock.lastCall).toMatchObject([
            FastingEndConfirmDialogComponent,
            { data: { title: 'FASTING.START_FAST_NOW_CONFIRM_TITLE' } },
        ]);
        expect(facade.skipCyclicDay).not.toHaveBeenCalled();

        getNativeButtonByText(fixture, 'FASTING.SKIP_DAY').click();
        expect(dialogService.open.mock.lastCall).toMatchObject([
            FastingEndConfirmDialogComponent,
            { data: { title: 'FASTING.SKIP_EATING_DAY_CONFIRM_TITLE' } },
        ]);
        expect(facade.postponeCyclicDay).toHaveBeenCalledOnce();
    });

    it('does not extend a long fast when the safety warning is cancelled', () => {
        facade.isActive.set(true);
        facade.currentSession.set({ ...createExtendedSession(), plannedDurationHours: WARNING_PLANNED_DURATION_HOURS });
        const result = new Subject<'confirm' | 'cancel'>();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => result });
        fixture.detectChanges();

        const panel = host(fixture).querySelector<HTMLButtonElement>('.fasting-controls__extend-panel-header');
        panel?.click();
        fixture.detectChanges();
        expect(panel?.getAttribute('aria-expanded')).toBe('true');
        getNativeButtonByText(fixture, 'FASTING.ADD_36_HOURS').click();
        expect(dialogService.open.mock.lastCall).toMatchObject([
            FastingSafetyDialogComponent,
            { data: { tone: 'warning', title: 'FASTING.EXTEND_WARNING_TITLE' } },
        ]);

        result.next('cancel');
        result.complete();
        expect(facade.extendByHours).not.toHaveBeenCalled();
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

        component['showCustomExtend']();
        component['showCustomReduce']();
        fixture.detectChanges();

        const text = host(fixture).textContent;
        expect(text).toContain('FASTING.ADD_TIME');
        expect(text).toContain('FASTING.REDUCE_TIME');

        component['extendByCustom']();
        component['reduceByCustom']();

        expect(facade.extendByHours).toHaveBeenCalledWith(CUSTOM_EXTEND_HOURS);
        expect(facade.reduceTargetByHours).toHaveBeenCalledWith(CUSTOM_REDUCE_HOURS);
    });

    it('asks for safety confirmation before extending beyond the warning threshold', () => {
        facade.isActive.set(true);
        facade.currentSession.set({ ...createExtendedSession(), plannedDurationHours: WARNING_PLANNED_DURATION_HOURS });
        facade.extendHours.set(CUSTOM_EXTEND_HOURS);
        fixture.detectChanges();

        component['extendByCustom']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.extendByHours).toHaveBeenCalledWith(CUSTOM_EXTEND_HOURS);
    });

    it('blocks extensions beyond the hard stop threshold', () => {
        facade.isActive.set(true);
        facade.currentSession.set({ ...createExtendedSession(), plannedDurationHours: HARD_STOP_PLANNED_DURATION_HOURS });
        fixture.detectChanges();

        component['onExtendHoursChange'](HARD_STOP_EXTEND_HOURS);
        component['extendByCustom']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.extendByHours).not.toHaveBeenCalled();
    });
});

describe('Fasting confirmation after the active session changes', () => {
    it.each(['replaced', 'ended'] as const)('discards an intermittent end confirmation after the session is %s', change => {
        const intermittent: FastingSession = {
            ...createExtendedSession(),
            id: entityId<'fasting-session'>('intermittent-session'),
            planType: 'Intermittent',
            protocol: 'Fast16Eat8',
        };
        facade.isActive.set(true);
        facade.currentSession.set(intermittent);
        const result = new Subject<'confirm' | 'cancel'>();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => result });
        fixture.detectChanges();

        getNativeButtonByText(fixture, 'FASTING.END_FAST').click();
        expect(dialogService.open.mock.lastCall).toMatchObject([
            FastingEndConfirmDialogComponent,
            { data: { title: 'FASTING.END_CONFIRM_TITLE' } },
        ]);
        facade.currentSession.set(
            change === 'replaced'
                ? { ...intermittent, id: entityId<'fasting-session'>('replacement-intermittent') }
                : { ...intermittent, endedAtUtc: utcInstant('2026-04-12T10:00:00Z') },
        );
        result.next('confirm');
        result.complete();

        expect(facade.endFasting).not.toHaveBeenCalled();
    });

    it('allows a new skip confirmation for the current cycle without applying the previous one', () => {
        const session = createCyclicSession();
        facade.isActive.set(true);
        facade.currentSession.set(session);
        const result = new Subject<'confirm' | 'cancel'>();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => result });
        fixture.detectChanges();

        getNativeButtonByText(fixture, 'FASTING.SKIP_FASTING_PERIOD').click();
        facade.currentSession.set({ ...session, id: entityId<'fasting-session'>('replacement-cycle') });
        result.next('confirm');
        result.complete();
        expect(facade.skipCyclicDay).not.toHaveBeenCalled();

        fixture.detectChanges();
        getNativeButtonByText(fixture, 'FASTING.SKIP_FASTING_PERIOD').click();
        expect(facade.skipCyclicDay).toHaveBeenCalledOnce();
    });
});

describe('Fasting safety approval after the extension target changes', () => {
    it.each(['replaced', 'retargeted'] as const)('keeps the active session unchanged after it is %s', change => {
        const session = { ...createExtendedSession(), plannedDurationHours: WARNING_PLANNED_DURATION_HOURS };
        facade.isActive.set(true);
        facade.currentSession.set(session);
        const result = new Subject<'confirm' | 'cancel'>();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => result });
        fixture.detectChanges();

        host(fixture).querySelector<HTMLButtonElement>('.fasting-controls__extend-panel-header')?.click();
        fixture.detectChanges();
        getNativeButtonByText(fixture, 'FASTING.ADD_36_HOURS').click();
        expect(dialogService.open).toHaveBeenCalledOnce();
        facade.currentSession.set(
            change === 'replaced'
                ? { ...session, id: entityId<'fasting-session'>('replacement-extended') }
                : { ...session, plannedDurationHours: HARD_STOP_PLANNED_DURATION_HOURS },
        );
        result.next('confirm');
        result.complete();

        expect(facade.extendByHours).not.toHaveBeenCalled();
    });
});

describe('FastingControlsComponent target reductions', () => {
    it('does not request unavailable target reductions', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createExtendedSession());
        facade.maxReducibleHours.set(SHORT_REDUCIBLE_HOURS);
        facade.reduceHours.set(DEFAULT_REDUCE_HOURS);
        fixture.detectChanges();

        component['reduceBy4Hours']();
        component['reduceBy8Hours']();
        component['reduceByCustom']();

        expect(facade.reduceTargetByHours).not.toHaveBeenCalled();
    });

    it('disables preset target reductions that exceed the remaining time', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createExtendedSession());
        facade.maxReducibleHours.set(SHORT_REDUCIBLE_HOURS);
        fixture.detectChanges();

        component['toggleReducePanel']();
        fixture.detectChanges();

        const reduce4Button = getNativeButtonByText(fixture, 'FASTING.REDUCE_4_HOURS');
        const reduce8Button = getNativeButtonByText(fixture, 'FASTING.REDUCE_8_HOURS');

        expect(reduce4Button.disabled).toBe(true);
        expect(reduce4Button.title).toBe('DISABLED_HINTS.FASTING_REDUCE_UNAVAILABLE');
        expect(reduce8Button.disabled).toBe(true);
        expect(reduce8Button.title).toBe('DISABLED_HINTS.FASTING_REDUCE_UNAVAILABLE');
    });

    it('allows custom target reduction when it fits the remaining time', () => {
        facade.isActive.set(true);
        facade.currentSession.set(createExtendedSession());
        facade.maxReducibleHours.set(SHORT_REDUCIBLE_HOURS);
        facade.reduceHours.set(SHORT_REDUCIBLE_HOURS);
        fixture.detectChanges();

        component['reduceByCustom']();

        expect(facade.reduceTargetByHours).toHaveBeenCalledWith(SHORT_REDUCIBLE_HOURS);
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

function getNativeButtonByText(componentFixture: ComponentFixture<FastingControlsComponent>, text: string): HTMLButtonElement {
    const buttonHost = getButtonByText(componentFixture, text);
    const button = buttonHost.querySelector('button');
    if (button === null) {
        throw new Error(`Native button with text "${text}" was not found.`);
    }

    return button;
}

function clickRadio(group: string, label: string): void {
    const options = host(fixture).querySelectorAll<HTMLButtonElement>(`[role="radiogroup"][aria-label="${group}"] button[role="radio"]`);
    const option = Array.from(options).find(button => button.textContent.trim() === label);
    if (option === undefined) {
        throw new Error(`No fasting radio option "${label}" in "${group}"`);
    }
    option.click();
    fixture.detectChanges();
}

function enterNumber(label: string, value: number): void {
    const labels = Array.from(host(fixture).querySelectorAll('label'));
    const numberLabel = labels.find(candidate => candidate.textContent.trim() === label);
    const input = numberLabel?.control;
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`No fasting number input "${label}"`);
    }
    input.value = String(value);
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
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
    maxReducibleHours: WritableSignal<number>;
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
        selectedProtocol: signal<FastingProtocol>('Fast16Eat8'),
        customHours: signal(INTERMITTENT_FAST_HOURS),
        customIntermittentFastHours: signal(INTERMITTENT_FAST_HOURS),
        cyclicEatDayProtocol: signal<FastingProtocol>('Fast16Eat8'),
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
        maxReducibleHours: signal(DEFAULT_REDUCE_HOURS),
    };
}

function createCyclicSession(): FastingSession {
    return {
        id: entityId<'fasting-session'>('session-1'),
        startedAtUtc: utcInstant('2026-04-12T06:00:00Z'),
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
        id: entityId<'fasting-session'>('extended-session-1'),
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
