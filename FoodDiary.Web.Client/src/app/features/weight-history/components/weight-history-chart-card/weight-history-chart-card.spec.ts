import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { WeightHistoryChartPoint } from '../../lib/weight-history-chart.mapper';
import { WeightHistoryChartCardComponent } from './weight-history-chart-card';

const CHART_VALUE = 74.2;
const DESIRED_WEIGHT = 70;

describe('WeightHistoryChartCardComponent', () => {
    it('keeps a stable chart viewport while loading', async () => {
        const { fixture } = await setupComponentAsync([], true);

        expect((fixture.nativeElement as HTMLElement).querySelector('.weight-history-page__chart-viewport')).not.toBeNull();
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.LOADING');
    });

    it('keeps existing chart content mounted under the loading overlay', async () => {
        const { fixture } = await setupComponentAsync([{ label: '2026-05-15', value: CHART_VALUE }], true);
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(root.querySelector('.weight-history-page__chart-loading')).not.toBeNull();
        expect(root.querySelector('.weight-history-page__chart--loading')).not.toBeNull();
    });

    it('derives empty state from chart data labels', async () => {
        const { component, fixture } = await setupComponentAsync([]);

        expect(component['hasPoints']()).toBe(false);
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.EMPTY_TITLE');
    });

    it('offers the latest measurement when the selected period is empty', async () => {
        const { component, fixture } = await setupComponentAsync([]);
        const showLatest = vi.fn();
        component.showLatest.subscribe(showLatest);
        fixture.componentRef.setInput('latestValue', CHART_VALUE);
        fixture.componentRef.setInput('latestDate', '2026-06-20');
        fixture.detectChanges();
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.EMPTY_PERIOD_TITLE');
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button')?.click();
        expect(showLatest).toHaveBeenCalledOnce();
    });

    it('detects chart points without a separate input', async () => {
        const { component } = await setupComponentAsync([{ label: '2026-05-15', value: CHART_VALUE }], true);

        expect(component['hasPoints']()).toBe(true);
    });

    it('passes the desired weight to the chart as a labeled reference line', async () => {
        const { component, fixture } = await setupComponentAsync([{ label: '2026-05-15', value: CHART_VALUE }]);

        expect(component['referenceLines']()).toEqual([{ value: DESIRED_WEIGHT, label: 'Goal: 70 kg' }]);
        expect((fixture.nativeElement as HTMLElement).querySelector('.fd-ui-line-chart__reference-label')?.textContent).toContain(
            'Goal: 70 kg',
        );
    });
});

async function setupComponentAsync(
    chartPoints: readonly WeightHistoryChartPoint[],
    isLoading = false,
): Promise<{ component: WeightHistoryChartCardComponent; fixture: ComponentFixture<WeightHistoryChartCardComponent> }> {
    await TestBed.configureTestingModule({
        imports: [WeightHistoryChartCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(WeightHistoryChartCardComponent);
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.componentRef.setInput('chartPoints', chartPoints);
    fixture.componentRef.setInput('desiredWeightKg', DESIRED_WEIGHT);
    fixture.componentRef.setInput('goalLabel', 'Goal: 70 kg');
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WeightHistoryChartCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
