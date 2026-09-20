import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { WaistHistoryChartPoint } from '../../lib/waist-history-chart.mapper';
import { WaistHistoryChartCardComponent } from './waist-history-chart-card';

const CHART_VALUE = 82;

describe('WaistHistoryChartCardComponent', () => {
    it('derives empty state from chart data labels', async () => {
        const { component, fixture } = await setupComponentAsync([]);

        expect(component['hasPoints']()).toBe(false);
        expect(getText(fixture)).toContain('WAIST_HISTORY.EMPTY_TITLE');
    });

    it('offers the latest measurement when the selected period is empty', async () => {
        const { component, fixture } = await setupComponentAsync([]);
        const showLatest = vi.fn();
        component.showLatest.subscribe(showLatest);
        fixture.componentRef.setInput('latestValue', CHART_VALUE);
        fixture.componentRef.setInput('latestDate', '2026-06-20');
        fixture.detectChanges();
        expect(getText(fixture)).toContain('WAIST_HISTORY.EMPTY_PERIOD_TITLE');
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button')?.click();
        expect(showLatest).toHaveBeenCalledOnce();
    });

    it('detects chart points without a separate input', async () => {
        const { component } = await setupComponentAsync([{ label: '2026-05-15', value: CHART_VALUE }], true);

        expect(component['hasPoints']()).toBe(true);
    });
});

async function setupComponentAsync(
    chartPoints: readonly WaistHistoryChartPoint[],
    isLoading = false,
): Promise<{ component: WaistHistoryChartCardComponent; fixture: ComponentFixture<WaistHistoryChartCardComponent> }> {
    await TestBed.configureTestingModule({
        imports: [WaistHistoryChartCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(WaistHistoryChartCardComponent);
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.componentRef.setInput('chartPoints', chartPoints);
    fixture.componentRef.setInput('desiredWaistCm', null);
    fixture.componentRef.setInput('goalLabel', 'Goal');
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WaistHistoryChartCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
