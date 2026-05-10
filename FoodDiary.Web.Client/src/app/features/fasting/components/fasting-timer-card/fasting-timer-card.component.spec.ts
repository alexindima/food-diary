import { Component, computed, type Signal, signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../services/localization.service';
import { FastingFacade } from '../../lib/fasting.facade';
import type { FastingSession } from '../../models/fasting.data';
import { FastingTimerCardComponent } from './fasting-timer-card.component';

let localizationLanguage: string;

describe('FastingTimerCardComponent', () => {
    beforeEach(() => {
        localizationLanguage = 'ru';
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-04-12T06:00:00Z'));
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('renders dashboard layout without page controls', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('dashboard');
        setSession(fixture, createSession());
        fixture.detectChanges();

        expect(fixture.debugElement.query(By.css('fd-dashboard-widget-frame'))).not.toBeNull();
        expect(fixture.debugElement.query(By.css('fd-fasting-controls'))).toBeNull();
    });

    it('renders dashboard layout without a fasting facade provider', async () => {
        const fixture = await createHostFixtureWithoutFacadeAsync();

        fixture.componentInstance.session.set(createSession());
        fixture.detectChanges();

        expect(fixture.debugElement.query(By.css('fd-dashboard-widget-frame'))).not.toBeNull();
        expect(fixture.debugElement.query(By.css('fd-fasting-controls'))).toBeNull();
    });

    it('renders page controls in page layout', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('page');
        fixture.detectChanges();

        expect(fixture.debugElement.query(By.css('fd-fasting-controls'))).not.toBeNull();
    });

    it('refreshes computed summary labels when the active language changes', async () => {
        const fixture = await createHostFixtureAsync();
        const translateService = TestBed.inject(TranslateService);
        translateService.setTranslation('ru', { FASTING: { FASTING_WINDOW: 'Пост' } });
        translateService.setTranslation('en', { FASTING: { FASTING_WINDOW: 'Fasting' } });

        await firstValueFrom(translateService.use('ru'));
        fixture.componentInstance.layout.set('dashboard');
        setSession(fixture, createSession());
        fixture.detectChanges();

        expect(host(fixture).textContent).toContain('Пост');

        localizationLanguage = 'en';
        await firstValueFrom(translateService.use('en'));
        fixture.detectChanges();

        expect(host(fixture).textContent).toContain('Fasting');
    });

    it.each(['dashboard', 'page'] as const)('does not render fasting stage details for eating phases in %s layout', async layout => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set(layout);
        setSession(
            fixture,
            createSession({
                planType: 'Cyclic',
                protocol: 'Cyclic',
                occurrenceKind: 'EatDay',
                cyclicFastDays: 1,
                cyclicEatDays: 1,
                cyclicEatDayFastHours: 16,
                cyclicEatDayEatingWindowHours: 8,
                cyclicPhaseDayNumber: 1,
                cyclicPhaseDayTotal: 1,
            }),
        );
        fixture.detectChanges();

        expect(fixture.debugElement.query(By.css('.fasting-timer-card__stage-title'))).toBeNull();
        expect(fixture.debugElement.query(By.css('.fasting-timer-card__next-stage-label'))).toBeNull();
    });

    it.each(['dashboard', 'page'] as const)('renders protocol separator without mojibake in %s layout', async layout => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set(layout);
        setSession(fixture, createSession({ protocol: 'F16_8' }));
        fixture.detectChanges();

        const separator = requireElement<HTMLElement>(fixture, '.fasting-timer-card__summary-protocol-separator');
        expect(separator.textContent.trim()).toBe('\u00b7');
        expect(host(fixture).textContent).not.toContain('\u00c2');
    });

    it.each(['dashboard', 'page'] as const)('clamps rendered progress percent to the valid ring range in %s layout', async layout => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set(layout);
        setSession(fixture, createExtendedSession({ startedAtUtc: getStartedAtUtc(30) }));
        fixture.detectChanges();

        const percent = requireElement<HTMLElement>(fixture, '.fasting-timer-card__percent');
        expect(percent.textContent.trim()).toBe('100%');
    });

    it.each(['dashboard', 'page'] as const)('uses the same progress ring geometry in %s layout', async layout => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set(layout);
        setSession(fixture, createExtendedSession({ startedAtUtc: getStartedAtUtc(6) }));
        fixture.detectChanges();

        const progressRing = requireElement<SVGCircleElement>(fixture, '.fasting-timer-card__ring-progress');
        const circumference = 2 * Math.PI * 90;
        expect(Number(progressRing.style.strokeDasharray)).toBeCloseTo(circumference);
        expect(Number(progressRing.style.strokeDashoffset)).toBeCloseTo(circumference * 0.75);
    });

    it.each(['dashboard', 'page'] as const)('uses shared summary ring content in %s layout', async layout => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set(layout);
        setSession(fixture, createSession());
        fixture.detectChanges();

        expect(host(fixture).querySelector('.fasting-timer-card__ring-content--summary')).not.toBeNull();
    });

    it('builds timer display from session inputs', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('dashboard');
        setSession(
            fixture,
            createExtendedSession({ plannedDurationHours: 10, initialPlannedDurationHours: 10, startedAtUtc: getStartedAtUtc(5) }),
        );
        fixture.detectChanges();

        const elapsed = requireElement<HTMLElement>(fixture, '.fasting-timer-card__elapsed');
        const percent = requireElement<HTMLElement>(fixture, '.fasting-timer-card__percent');
        expect(elapsed.textContent.trim()).toBe('05:00:00');
        expect(percent.textContent.trim()).toBe('50%');
    });

    it('uses facade elapsed time in page layout', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('page');
        setSession(
            fixture,
            createExtendedSession({ plannedDurationHours: 10, initialPlannedDurationHours: 10, startedAtUtc: getStartedAtUtc(1) }),
        );
        getFacadeStub(fixture).elapsedMs.set(5 * 3_600_000);
        fixture.detectChanges();

        const elapsed = requireElement<HTMLElement>(fixture, '.fasting-timer-card__elapsed');
        const percent = requireElement<HTMLElement>(fixture, '.fasting-timer-card__percent');
        expect(elapsed.textContent.trim()).toBe('05:00:00');
        expect(percent.textContent.trim()).toBe('50%');
    });

    it('advances dashboard elapsed time without a fasting facade provider', async () => {
        const fixture = await createHostFixtureWithoutFacadeAsync();

        fixture.componentInstance.session.set(createExtendedSession({ plannedDurationHours: 1, initialPlannedDurationHours: 1 }));
        fixture.detectChanges();
        vi.advanceTimersByTime(1_000);
        fixture.detectChanges();

        const elapsed = requireElement<HTMLElement>(fixture, '.fasting-timer-card__elapsed');
        expect(elapsed.textContent.trim()).toBe('00:00:01');
    });

    it('marks progress rings as decorative', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('dashboard');
        fixture.detectChanges();

        const ringSvg = requireElement<SVGElement>(fixture, '.fasting-timer-card__ring-svg');
        expect(ringSvg.getAttribute('aria-hidden')).toBe('true');
        expect(ringSvg.getAttribute('focusable')).toBe('false');
    });
});

@Component({
    imports: [FastingTimerCardComponent],
    template: '<fd-fasting-timer-card [layout]="layout()" [session]="session()" />',
})
class FastingTimerCardHostComponent {
    public readonly layout = signal<'dashboard' | 'page'>('dashboard');
    public readonly session = signal<FastingSession | null>(null);
}

async function createHostFixtureAsync(): Promise<ComponentFixture<FastingTimerCardHostComponent>> {
    await TestBed.configureTestingModule({
        imports: [FastingTimerCardHostComponent, TranslateModule.forRoot()],
        providers: [
            { provide: FastingFacade, useValue: createFastingFacadeStub() },
            {
                provide: FdUiDialogService,
                useValue: createDialogServiceStub(),
            },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => localizationLanguage } },
        ],
    }).compileComponents();

    return TestBed.createComponent(FastingTimerCardHostComponent);
}

async function createHostFixtureWithoutFacadeAsync(): Promise<ComponentFixture<FastingTimerCardHostComponent>> {
    await TestBed.configureTestingModule({
        imports: [FastingTimerCardHostComponent, TranslateModule.forRoot()],
        providers: [
            {
                provide: FdUiDialogService,
                useValue: createDialogServiceStub(),
            },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => localizationLanguage } },
        ],
    }).compileComponents();

    return TestBed.createComponent(FastingTimerCardHostComponent);
}

function createDialogServiceStub(): Pick<FdUiDialogService, 'open'> {
    return {
        open: () => ({
            afterClosed: () => ({
                pipe: () => ({
                    subscribe: (): undefined => undefined,
                }),
            }),
        }),
    } as unknown as Pick<FdUiDialogService, 'open'>;
}

interface FastingFacadeStub {
    isActive: Signal<boolean>;
    currentSession: WritableSignal<FastingSession | null>;
    selectedMode: WritableSignal<string>;
    selectedProtocol: WritableSignal<string>;
    customHours: WritableSignal<number>;
    customIntermittentFastHours: WritableSignal<number>;
    cyclicEatDayProtocol: WritableSignal<string>;
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
    canExtendActiveSession: Signal<boolean>;
    elapsedMs: WritableSignal<number>;
    selectMode: () => void;
    selectProtocol: () => void;
    setCustomHours: () => void;
    setCustomIntermittentFastHours: () => void;
    setCyclicPreset: () => void;
    selectCustomCyclicPreset: () => void;
    setCyclicFastDays: () => void;
    setCyclicEatDays: () => void;
    selectCyclicEatDayProtocol: () => void;
    setCyclicEatDayFastHours: () => void;
    startFasting: () => void;
    endFasting: () => void;
    setExtendHours: () => void;
    setReduceHours: () => void;
    extendByHours: () => void;
    reduceTargetByHours: () => void;
    skipCyclicDay: () => void;
    postponeCyclicDay: () => void;
}

function createFastingFacadeStub(): FastingFacadeStub {
    const currentSession = signal<FastingSession | null>(null);

    return {
        isActive: computed(() => currentSession() !== null && currentSession()?.endedAtUtc === null),
        currentSession,
        selectedMode: signal('intermittent'),
        selectedProtocol: signal('F16_8'),
        customHours: signal(16),
        customIntermittentFastHours: signal(16),
        cyclicEatDayProtocol: signal('F16_8'),
        cyclicFastDays: signal(1),
        cyclicEatDays: signal(1),
        cyclicUsesCustomPreset: signal(false),
        cyclicEatDayFastHours: signal(16),
        extendHours: signal(24),
        reduceHours: signal(4),
        isStarting: signal(false),
        isEnding: signal(false),
        isExtending: signal(false),
        isReducing: signal(false),
        isUpdatingCycle: signal(false),
        canExtendActiveSession: computed(() => false),
        elapsedMs: signal(0),
        selectMode: () => undefined,
        selectProtocol: () => undefined,
        setCustomHours: () => undefined,
        setCustomIntermittentFastHours: () => undefined,
        setCyclicPreset: () => undefined,
        selectCustomCyclicPreset: () => undefined,
        setCyclicFastDays: () => undefined,
        setCyclicEatDays: () => undefined,
        selectCyclicEatDayProtocol: () => undefined,
        setCyclicEatDayFastHours: () => undefined,
        startFasting: () => undefined,
        endFasting: () => undefined,
        setExtendHours: () => undefined,
        setReduceHours: () => undefined,
        extendByHours: () => undefined,
        reduceTargetByHours: () => undefined,
        skipCyclicDay: () => undefined,
        postponeCyclicDay: () => undefined,
    };
}

function getFacadeStub(fixture: ComponentFixture<FastingTimerCardHostComponent>): FastingFacadeStub {
    return fixture.debugElement.injector.get(FastingFacade) as unknown as FastingFacadeStub;
}

function host(fixture: ComponentFixture<FastingTimerCardHostComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function requireElement<T extends Element>(fixture: ComponentFixture<FastingTimerCardHostComponent>, selector: string): T {
    const element = host(fixture).querySelector<T>(selector);
    if (element === null) {
        throw new Error(`Expected element ${selector} to exist.`);
    }

    return element;
}

function setSession(fixture: ComponentFixture<FastingTimerCardHostComponent>, session: FastingSession | null): void {
    fixture.componentInstance.session.set(session);
    getFacadeStub(fixture).currentSession.set(session);
    getFacadeStub(fixture).elapsedMs.set(
        session ? Math.max(0, new Date('2026-04-12T06:00:00Z').getTime() - new Date(session.startedAtUtc).getTime()) : 0,
    );
}

function createExtendedSession(overrides: Partial<FastingSession> = {}): FastingSession {
    return createSession({
        planType: 'Extended',
        protocol: 'F24_0',
        initialPlannedDurationHours: 24,
        plannedDurationHours: 24,
        ...overrides,
    });
}

function getStartedAtUtc(hoursAgo: number): string {
    return new Date(new Date('2026-04-12T06:00:00Z').getTime() - hoursAgo * 3_600_000).toISOString();
}

function createSession(overrides: Partial<FastingSession> = {}): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-04-12T06:00:00Z',
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
        ...overrides,
    };
}
