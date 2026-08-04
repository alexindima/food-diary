import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WeightTrendCardComponent, type WeightTrendPoint } from './weight-trend-card';

const CURRENT_WEIGHT = 80;
const WEIGHT_CHANGE = -1.24;
const EXPECTED_FORMATTED_CHANGE = '-1.2';
const INTERMEDIATE_WEIGHT_OFFSET = 0.5;
const MINIMUM_CHART_POINTS = 3;
const MINIMUM_CHART_BOUND_OFFSET = 1;
const MAXIMUM_CHART_BOUND_OFFSET = 2;

describe('WeightTrendCardComponent', () => {
    it('formats change tone and value', async () => {
        const { component, fixture } = await setupComponentAsync({ change: WEIGHT_CHANGE });

        fixture.detectChanges();

        expect(component['changeTone']()).toBe('positive');
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
});

async function setupComponentAsync(
    overrides: Partial<{
        currentWeight: number | null;
        change: number | null;
        timeframeLabel: string;
        points: WeightTrendPoint[];
        isLoading: boolean;
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

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
