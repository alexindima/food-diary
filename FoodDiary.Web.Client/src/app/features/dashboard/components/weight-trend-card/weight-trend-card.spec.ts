import { registerLocaleData } from '@angular/common';
import ru from '@angular/common/locales/ru';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WeightTrendCardComponent, type WeightTrendPoint } from './weight-trend-card';

registerLocaleData(ru);
const CURRENT_WEIGHT = 80;
const LOCALIZED_CURRENT = 80.5;
const IMPERIAL_CURRENT = 176.4;
const WEIGHT_CHANGE = -1.24;
const EXPECTED_FORMATTED_CHANGE = '-1.2';
const INTERMEDIATE_WEIGHT_OFFSET = 0.5;
const MINIMUM_CHART_POINTS = 3;
const MINIMUM_CHART_BOUND_OFFSET = 1;
const MAXIMUM_CHART_BOUND_OFFSET = 2;
const TARGET_WEIGHT = 72;

describe('WeightTrendCardComponent', () => {
    it('formats the change value', async () => {
        const { component, fixture } = await setupComponentAsync({ change: WEIGHT_CHANGE });

        fixture.detectChanges();

        expect(component['formattedChangeValue']()).toBe(EXPECTED_FORMATTED_CHANGE);
    });

    it('builds chart points from ordered weight points', async () => {
        const { component, fixture } = await setupComponentAsync({
            points: [
                { date: '2026-05-03', value: CURRENT_WEIGHT },
                { date: '2026-05-01', value: CURRENT_WEIGHT - 1 },
                { date: '2026-05-02', value: null },
            ],
        });

        fixture.detectChanges();

        expect(component['measurementPoints']()).toEqual([
            { label: 'May 1', value: CURRENT_WEIGHT - 1, xPosition: 0 },
            { label: 'May 3', value: CURRENT_WEIGHT, xPosition: 1 },
        ]);
        expect(component['measurementCount']()).toBe(2);
        expect(component['showChart']()).toBe(false);
    });

    it('shows the chart only after three measurements', async () => {
        const { component, fixture } = await setupComponentAsync({
            points: [
                { date: '2026-05-01', value: CURRENT_WEIGHT + 1 },
                { date: '2026-05-02', value: CURRENT_WEIGHT + INTERMEDIATE_WEIGHT_OFFSET },
                { date: '2026-05-03', value: CURRENT_WEIGHT },
            ],
        });

        fixture.detectChanges();

        expect(component['measurementCount']()).toBe(MINIMUM_CHART_POINTS);
        expect(component['showChart']()).toBe(true);
        expect(component['measurementPoints']()).toEqual([
            { label: 'May 1', value: CURRENT_WEIGHT + 1, xPosition: 0 },
            { label: 'May 2', value: CURRENT_WEIGHT + INTERMEDIATE_WEIGHT_OFFSET, xPosition: INTERMEDIATE_WEIGHT_OFFSET },
            { label: 'May 3', value: CURRENT_WEIGHT, xPosition: 1 },
        ]);
        expect(component['chartBounds']()).toEqual({
            minimum: CURRENT_WEIGHT - MINIMUM_CHART_BOUND_OFFSET,
            maximum: CURRENT_WEIGHT + MAXIMUM_CHART_BOUND_OFFSET,
        });
    });

    it('uses the configured empty-state translation key', async () => {
        const { fixture } = await setupComponentAsync();
        fixture.componentRef.setInput('emptyStateKey', 'WAIST_CARD.NO_DATA');

        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).textContent).toContain('WAIST_CARD.NO_DATA');
        expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('WEIGHT_TREND_CARD.NO_DATA');
    });

    it('labels an optional target on the compact chart', async () => {
        const { component, fixture } = await setupComponentAsync({
            targetValue: TARGET_WEIGHT,
            points: [
                { date: '2026-05-01', value: CURRENT_WEIGHT + 1 },
                { date: '2026-05-02', value: CURRENT_WEIGHT + INTERMEDIATE_WEIGHT_OFFSET },
                { date: '2026-05-03', value: CURRENT_WEIGHT },
            ],
        });

        fixture.detectChanges();

        expect(component['referenceLines']()).toEqual([
            expect.objectContaining({ value: TARGET_WEIGHT, color: 'var(--fd-color-blue-500)' }),
        ]);
        expect((fixture.nativeElement as HTMLElement).querySelector('.fd-ui-line-chart__reference-line')).not.toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('.weight-trend-card__goal')).not.toBeNull();
    });
});

describe('Weight and waist rendered regression states', () => {
    for (const kind of ['weight', 'length'] as const) {
        it.each([
            { count: 0, expected: 'WEIGHT_TREND_CARD.EMPTY_TITLE' },
            { count: 1, expected: 'WEIGHT_TREND_CARD.FIRST_MEASUREMENT' },
            { count: 2, expected: 'WEIGHT_TREND_CARD.TWO_MEASUREMENTS' },
            { count: 3, expected: 'WEIGHT_TREND_CARD.STABLE_TREND' },
        ])(`${kind}: renders useful text without a misleading graph for $count measurements`, async ({ count, expected }) => {
            const { fixture } = await setupComponentAsync({
                points: Array.from({ length: count }, (_, index) => ({ date: `2026-05-0${index + 1}`, value: CURRENT_WEIGHT })),
            });
            fixture.componentRef.setInput('measurementKind', kind);
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            expect(host.querySelector('fd-ui-line-chart')).toBeNull();
            expect(host.textContent).toContain(expected);
            expect(host.textContent).not.toMatch(/NaN|Infinity/u);
        });

        it(`${kind}: keeps the goal outside the plot and removes it when no target exists`, async () => {
            const { fixture } = await setupComponentAsync({
                targetValue: TARGET_WEIGHT,
                points: [
                    { date: '2026-05-01', value: CURRENT_WEIGHT + 1 },
                    { date: '2026-05-02', value: CURRENT_WEIGHT + INTERMEDIATE_WEIGHT_OFFSET },
                    { date: '2026-05-03', value: CURRENT_WEIGHT },
                ],
            });
            fixture.componentRef.setInput('measurementKind', kind);
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            const plot = host.querySelector('.weight-trend-card__chart');
            expect(plot?.nextElementSibling?.classList.contains('weight-trend-card__goal')).toBe(true);
            expect(plot?.querySelector('.weight-trend-card__goal')).toBeNull();
            fixture.componentRef.setInput('targetValue', null);
            fixture.detectChanges();
            expect(host.querySelector<HTMLElement>('.weight-trend-card__goal')?.style.display).toBe('none');
            expect(host.querySelector('.fd-ui-line-chart__reference-line')).toBeNull();
        });

        it(`${kind}: changes numeric formatting from English to Russian without remounting`, async () => {
            const { fixture } = await setupComponentAsync({
                currentWeight: LOCALIZED_CURRENT,
                change: WEIGHT_CHANGE,
                points: [
                    { date: '2026-05-01', value: CURRENT_WEIGHT + 1 },
                    { date: '2026-05-02', value: LOCALIZED_CURRENT },
                ],
            });
            fixture.componentRef.setInput('measurementKind', kind);
            const translate = TestBed.inject(TranslateService);
            translate.use('en');
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            expect(host.querySelector('.weight-trend-card__value')?.textContent.trim()).toBe('80.5');
            expect(host.querySelector('.weight-trend-card__delta')?.textContent).toContain('-1.2');
            translate.use('ru');
            fixture.detectChanges();
            expect(host.querySelector('.weight-trend-card__value')?.textContent.trim()).toBe('80,5');
            expect(host.querySelector('.weight-trend-card__delta')?.textContent).toContain('-1,2');
        });
    }

    it('updates values, units and reference lines together when switching measurement systems', async () => {
        const { component, fixture } = await setupComponentAsync({
            targetValue: CURRENT_WEIGHT,
            points: [{ date: '2026-05-01', value: CURRENT_WEIGHT }],
        });
        fixture.detectChanges();
        const measurements = TestBed.inject(MeasurementSystemService);
        measurements.setSystem('imperial');
        fixture.detectChanges();
        expect(component['displayCurrentValue']()).toBe(IMPERIAL_CURRENT);
        expect(component['referenceLines']()[0].value).toBe(IMPERIAL_CURRENT);
        expect((fixture.nativeElement as HTMLElement).querySelector('.weight-trend-card__unit')?.textContent).toContain('GENERAL.UNITS.LB');
        measurements.setSystem('metric');
        fixture.detectChanges();
        expect(component['displayCurrentValue']()).toBe(CURRENT_WEIGHT);
        expect(component['referenceLines']()[0].value).toBe(CURRENT_WEIGHT);
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        currentWeight: number | null;
        change: number | null;
        timeframeLabel: string;
        points: WeightTrendPoint[];
        isLoading: boolean;
        targetValue: number | null;
    }> = {},
): Promise<{
    component: WeightTrendCardComponent;
    fixture: ComponentFixture<WeightTrendCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [WeightTrendCardComponent],
            providers: [provideRouter([]), provideTranslateTesting()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(WeightTrendCardComponent);
    fixture.componentRef.setInput('currentWeight', overrides.currentWeight ?? CURRENT_WEIGHT);
    fixture.componentRef.setInput('change', overrides.change ?? null);
    fixture.componentRef.setInput('timeframeLabel', overrides.timeframeLabel ?? '30 days');
    fixture.componentRef.setInput('points', overrides.points ?? []);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('targetValue', overrides.targetValue ?? null);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
