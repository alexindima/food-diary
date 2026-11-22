import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import {
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_LACTATION,
    CYCLE_FACTOR_TYPE_NO_PERIOD,
    CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_PERIMENOPAUSE,
    CYCLE_FACTOR_TYPE_POSTPARTUM,
    CYCLE_FACTOR_TYPE_PREGNANCY,
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CycleFactor,
    type CyclePredictions,
    type CycleResponse,
    type MenstrualEpisode,
} from '../../../../shared/models/cycle.data';
import { calendarDate } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { CycleSummaryCardComponent } from './cycle-summary-card';

const CYCLE_DAY = 5;
const DAYS_TO_PERIOD = 24;
const RECORDED_CYCLE_DAY = 3;
const EPISODE: MenstrualEpisode = {
    id: entityId<'menstrual-episode'>('episode-1'),
    cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
    startDate: calendarDate('2026-05-01T00:00:00.000Z'),
    endDate: null,
    status: 1,
    excludedFromPredictions: false,
};
const CYCLE: CycleResponse = {
    id: entityId<'cycle-profile'>('cycle-1'),
    userId: entityId<'user'>('user-1'),
    mode: CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    goal: 0,
    reproductiveState: 0,
    hideFromDashboard: false,
    confidence: 1,
    trackingStartDate: calendarDate('2026-05-01T00:00:00.000Z'),
    averageCycleLength: 28,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: true,
    isOnboardingComplete: true,
    showFertilityEstimates: true,
    discreetNotifications: true,
    bleedingEntries: [],
    symptoms: [],
    factors: [
        {
            id: entityId<'cycle-factor'>('factor-1'),
            cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate('2026-05-02T00:00:00.000Z'),
            endDate: null,
            notes: null,
        },
    ],
    fertilitySignals: [],
    menstrualEpisodes: [EPISODE],
    predictions: null,
};

describe('CycleSummaryCard navigation', () => {
    it('exposes the configured card as a keyboard-accessible link to the cycle journal', async () => {
        const { fixture } = await setupComponentAsync();
        fixture.detectChanges();

        const link = (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('a[href="/cycle-tracking"]');
        expect(link, 'Configured cycle card must link to its journal').not.toBeNull();
        expect(link?.getAttribute('aria-label')).toBe('CYCLE_CARD.TITLE');
        expect(link?.tabIndex).toBe(0);
    });

    it.each([{ cycle: null }, { isLoading: true }])('does not cover setup or loading with a journal link: %j', async overrides => {
        const { fixture } = await setupComponentAsync(overrides);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('a[href="/cycle-tracking"]')).toBeNull();
    });
});

describe('CycleSummaryCardComponent', () => {
    it('calculates cycle day and next period status from inputs', async () => {
        const { component, fixture } = await setupComponentAsync({
            referenceDate: '2026-05-05',
            predictions: {
                ovulationFrom: null,
                ovulationTo: null,
                nextPeriodStartFrom: calendarDate('2026-05-29'),
                nextPeriodStartTo: calendarDate('2026-05-31'),
                pmsWindowStart: null,
                pmsWindowEnd: null,
                confidence: 'Moderate',
                rationale: 'Based on recent bleeding entries.',
            },
        });

        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { CYCLE_CARD: { NEXT_PERIOD_IN: 'Next period in {{days}} days' } });
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();

        expect(component['cycleDay']()).toBe(CYCLE_DAY);
        expect(getText(fixture)).toContain(`Next period in ${DAYS_TO_PERIOD} days`);
        const card = fixture.nativeElement as HTMLElement;
        expect(Array.from(card.querySelectorAll('.cycle-summary-card__pill'), pill => pill.textContent.trim())).toEqual([
            'CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE',
            'CYCLE_CARD.CONFIDENCE_MODERATE',
            'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
        ]);
    });

    it('replaces the configured journal with an actionable setup when the profile is removed', async () => {
        const { component, fixture } = await setupComponentAsync();
        const setupSpy = vi.fn();
        component.setupAction.subscribe(setupSpy);
        fixture.detectChanges();
        const card = fixture.nativeElement as HTMLElement;
        expect(card.querySelector('a[href="/cycle-tracking"]')).not.toBeNull();
        expect(card.querySelector('.cycle-summary-card__meta')).not.toBeNull();

        fixture.componentRef.setInput('cycle', null);
        fixture.detectChanges();

        expect(card.querySelector('a[href="/cycle-tracking"]')).toBeNull();
        expect(card.querySelector('.cycle-summary-card__meta')).toBeNull();
        const setup = card.querySelector<HTMLButtonElement>('.cycle-summary-card__setup button');
        expect(setup?.textContent).toContain('CYCLE_CARD.NO_DATA_ACTION');
        setup?.click();
        expect(setupSpy).toHaveBeenCalledOnce();
    });

    it('counts from the recorded period instead of the observation start at local midnight', async () => {
        const { component, fixture } = await setupComponentAsync({
            cycle: {
                ...CYCLE,
                trackingStartDate: calendarDate('2026-08-01'),
                menstrualEpisodes: [{ ...EPISODE, startDate: calendarDate('2026-10-16T00:00:00Z') }],
            },
            referenceDate: new Date('2026-10-18T00:00:00'),
        });

        fixture.detectChanges();

        expect(component['cycleDay']()).toBe(RECORDED_CYCLE_DAY);
        expect((fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.cycle-summary-card__day')?.hidden).toBe(false);
    });

    it('keeps a configured journal without episodes without inventing a numeric cycle day', async () => {
        const { component, fixture } = await setupComponentAsync({ cycle: { ...CYCLE, menstrualEpisodes: [] } });

        fixture.detectChanges();

        expect(component['cycleDay']()).toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.cycle-summary-card__day')?.hidden).toBe(true);
        expect(getText(fixture)).toContain('CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE');
        expect(getText(fixture)).not.toContain('CYCLE_CARD.NO_DATA_TITLE');
    });

    it.each([CYCLE_TRACKING_MODE_PREGNANCY, CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION, CYCLE_TRACKING_MODE_NO_PERIOD] as const)(
        'keeps metadata without a numeric menstrual day for mode %s',
        async mode => {
            const { component, fixture } = await setupComponentAsync({ cycle: { ...CYCLE, mode } });

            fixture.detectChanges();

            expect(component['cycleDay']()).toBeNull();
            expect((fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.cycle-summary-card__day')?.hidden).toBe(true);
        },
    );
});

describe('CycleSummaryCard prediction presentation', () => {
    it.each([
        ['2026-05-08', '2026-05-18', 'Ovulation in 3 days'],
        ['2026-05-05', '2026-05-18', 'Ovulation in 0 days'],
        ['2026-05-04', '2026-05-05', 'Period expected today'],
        ['2026-05-04', '2026-05-08', 'Next period in 3 days'],
        ['2026-05-04', null, undefined],
    ] as const)('presents the applicable prediction for ovulation=%s and period=%s', async (ovulation, period, text) => {
        const { fixture } = await setupComponentAsync({
            referenceDate: '2026-05-05',
            predictions: {
                ovulationFrom: calendarDate(ovulation),
                nextPeriodStartFrom: period === null ? null : calendarDate(period),
                confidence: 'Learning',
                rationale: 'Based on journal data',
            },
        });
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            CYCLE_CARD: {
                OVULATION_IN: 'Ovulation in {{days}} days',
                NEXT_PERIOD_IN: 'Next period in {{days}} days',
                NEXT_PERIOD_TODAY: 'Period expected today',
            },
        });
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();

        const card = fixture.nativeElement as HTMLElement;
        expect(card.querySelector('.cycle-summary-card__status')?.textContent.trim()).toBe(text);
    });

    it('omits the cycle day and prediction when the reference date is absent', async () => {
        const { fixture } = await setupComponentAsync({
            referenceDate: null,
            predictions: { ovulationFrom: calendarDate('2026-05-08'), confidence: 'High', rationale: '' },
        });
        fixture.detectChanges();

        const card = fixture.nativeElement as HTMLElement;
        expect(card.querySelector<HTMLElement>('.cycle-summary-card__day')?.hidden).toBe(true);
        expect(card.querySelector('.cycle-summary-card__status')).toBeNull();
        expect(card.querySelector('a[href="/cycle-tracking"]')).not.toBeNull();
    });
});

describe('CycleSummaryCard forecast across selected local days', () => {
    it('switches from ovulation to period without shifting cycle dates encoded at UTC midnight', async () => {
        const { fixture } = await setupComponentAsync({
            cycle: { ...CYCLE, trackingStartDate: calendarDate('2026-03-01') },
            referenceDate: new Date('2026-03-07T23:45:00'),
            predictions: {
                ovulationFrom: calendarDate('2026-03-09T00:00:00.000Z'),
                nextPeriodStartFrom: calendarDate('2026-03-11T00:00:00.000Z'),
                confidence: 'Learning',
                rationale: 'Based on journal data',
            },
        });
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            CYCLE_CARD: {
                OVULATION_IN: 'Ovulation in {{days}} days',
                NEXT_PERIOD_IN: 'Next period in {{days}} days',
                NEXT_PERIOD_TODAY: 'Period expected today',
            },
        });
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();
        expect(getText(fixture)).toContain('Ovulation in 2 days');

        for (const [localDate, headline] of [
            ['2026-03-09T00:10:00', 'Ovulation in 0 days'],
            ['2026-03-10T00:10:00', 'Next period in 1 days'],
            ['2026-03-11T00:10:00', 'Period expected today'],
        ]) {
            fixture.componentRef.setInput('referenceDate', new Date(localDate));
            fixture.detectChanges();
            const card = fixture.nativeElement as HTMLElement;
            expect(card.querySelector('.cycle-summary-card__status')?.textContent.trim()).toBe(headline);
        }
    });
});

describe('CycleSummaryCard recent factors', () => {
    it('shows the active mode and two newest open factors and omits completed factors', async () => {
        const cycleProfileId = CYCLE.id;
        const factors: CycleFactor[] = [
            {
                id: entityId<'cycle-factor'>('older'),
                type: CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
                cycleProfileId,
                startDate: calendarDate('2026-05-01'),
            },
            {
                id: entityId<'cycle-factor'>('completed'),
                type: CYCLE_FACTOR_TYPE_PREGNANCY,
                cycleProfileId,
                startDate: calendarDate('2026-05-04'),
                endDate: calendarDate('2026-05-04'),
            },
            {
                id: entityId<'cycle-factor'>('postpartum'),
                type: CYCLE_FACTOR_TYPE_POSTPARTUM,
                cycleProfileId,
                startDate: calendarDate('2026-05-02'),
                endDate: null,
            },
            {
                id: entityId<'cycle-factor'>('lactation'),
                type: CYCLE_FACTOR_TYPE_LACTATION,
                cycleProfileId,
                startDate: calendarDate('2026-05-03'),
            },
        ];
        const { fixture } = await setupComponentAsync({
            cycle: { ...CYCLE, mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING, factors },
            predictions: { confidence: 'Observed', rationale: '' },
        });
        fixture.detectChanges();

        const card = fixture.nativeElement as HTMLElement;
        expect(Array.from(card.querySelectorAll('.cycle-summary-card__pill'), pill => pill.textContent.trim())).toEqual([
            'CYCLE_TRACKING.MODE_PERIOD_TRACKING',
            'CYCLE_CARD.CONFIDENCE_UNKNOWN',
            'CYCLE_TRACKING.FACTOR_LACTATION',
            'CYCLE_TRACKING.FACTOR_POSTPARTUM',
        ]);
    });
});

describe('Cycle summary factors on the selected day', () => {
    it('includes factors through their end date and shows planned factors once their start day arrives', async () => {
        const factors: CycleFactor[] = [
            {
                id: entityId<'cycle-factor'>('ending-today'),
                cycleProfileId: CYCLE.id,
                type: CYCLE_FACTOR_TYPE_POSTPARTUM,
                startDate: calendarDate('2026-05-03'),
                endDate: calendarDate('2026-05-05T00:00:00.000Z'),
            },
            {
                id: entityId<'cycle-factor'>('ending-later'),
                cycleProfileId: CYCLE.id,
                type: CYCLE_FACTOR_TYPE_LACTATION,
                startDate: calendarDate('2026-05-02'),
                endDate: calendarDate('2026-05-09'),
            },
            {
                id: entityId<'cycle-factor'>('starts-later'),
                cycleProfileId: CYCLE.id,
                type: CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
                startDate: calendarDate('2026-05-07T00:00:00.000Z'),
                endDate: null,
            },
            {
                id: entityId<'cycle-factor'>('already-ended'),
                cycleProfileId: CYCLE.id,
                type: CYCLE_FACTOR_TYPE_PREGNANCY,
                startDate: calendarDate('2026-05-01'),
                endDate: calendarDate('2026-05-04'),
            },
        ];
        const { fixture } = await setupComponentAsync({ cycle: { ...CYCLE, factors }, referenceDate: '2026-05-05' });
        fixture.detectChanges();
        const card = fixture.nativeElement as HTMLElement;
        const visiblePills = (): string[] =>
            Array.from(card.querySelectorAll('.cycle-summary-card__pill'), pill => pill.textContent.trim());
        const mode = 'CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE';
        expect(visiblePills()).toEqual([mode, 'CYCLE_TRACKING.FACTOR_POSTPARTUM', 'CYCLE_TRACKING.FACTOR_LACTATION']);

        fixture.componentRef.setInput('referenceDate', '2026-05-07');
        fixture.detectChanges();
        expect(visiblePills()).toEqual([mode, 'CYCLE_TRACKING.FACTOR_NON_HORMONAL_CONTRACEPTION', 'CYCLE_TRACKING.FACTOR_LACTATION']);

        fixture.componentRef.setInput('referenceDate', '2026-05-01');
        fixture.detectChanges();
        expect(visiblePills()).toEqual([mode, 'CYCLE_TRACKING.FACTOR_PREGNANCY']);

        fixture.componentRef.setInput('referenceDate', null);
        fixture.detectChanges();
        expect(visiblePills()).toEqual([mode]);
    });

    it.each([
        [CYCLE_TRACKING_MODE_PERIMENOPAUSE, CYCLE_FACTOR_TYPE_PERIMENOPAUSE, 'PERIMENOPAUSE'],
        [CYCLE_TRACKING_MODE_NO_PERIOD, CYCLE_FACTOR_TYPE_NO_PERIOD, 'NO_PERIOD'],
    ] as const)('shows the configured mode and scheduled factor ending today (%s)', async (mode, type, label) => {
        const { fixture } = await setupComponentAsync({
            cycle: {
                ...CYCLE,
                mode,
                factors: [
                    {
                        id: entityId<'cycle-factor'>('ends-on-selected-day'),
                        cycleProfileId: CYCLE.id,
                        type,
                        startDate: calendarDate('2026-05-02'),
                        endDate: calendarDate('2026-05-05'),
                    },
                ],
            },
        });
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(Array.from(host.querySelectorAll('.cycle-summary-card__pill'), pill => pill.textContent.trim())).toEqual([
            `CYCLE_TRACKING.MODE_${label}`,
            `CYCLE_TRACKING.FACTOR_${label}`,
        ]);
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        cycle: CycleResponse | null;
        predictions: CyclePredictions | null;
        referenceDate: Date | string | null;
        isLoading: boolean;
    }> = {},
): Promise<{
    component: CycleSummaryCardComponent;
    fixture: ComponentFixture<CycleSummaryCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [CycleSummaryCardComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(CycleSummaryCardComponent);
    fixture.componentRef.setInput('cycle', 'cycle' in overrides ? overrides.cycle : CYCLE);
    fixture.componentRef.setInput('predictions', 'predictions' in overrides ? overrides.predictions : null);
    fixture.componentRef.setInput('referenceDate', 'referenceDate' in overrides ? overrides.referenceDate : '2026-05-05');
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<CycleSummaryCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
