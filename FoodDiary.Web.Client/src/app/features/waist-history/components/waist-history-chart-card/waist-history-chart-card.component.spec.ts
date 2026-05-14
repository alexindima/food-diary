import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { describe, expect, it } from 'vitest';

import { WaistHistoryChartCardComponent } from './waist-history-chart-card.component';

const CHART_VALUE = 82;

describe('WaistHistoryChartCardComponent', () => {
    it('derives empty state from chart data labels', async () => {
        const { component, fixture } = await setupComponentAsync({ labels: [], datasets: [] });

        expect(component.hasPoints()).toBe(false);
        expect(getText(fixture)).toContain('WAIST_HISTORY.NO_DATA_FOR_CHART');
    });

    it('detects chart points without a separate input', async () => {
        const { component } = await setupComponentAsync(
            {
                labels: ['2026-05-15'],
                datasets: [{ data: [CHART_VALUE], label: 'Waist' }],
            },
            true,
        );

        expect(component.hasPoints()).toBe(true);
    });
});

async function setupComponentAsync(
    chartData: ChartConfiguration<'line'>['data'],
    isLoading = false,
): Promise<{ component: WaistHistoryChartCardComponent; fixture: ComponentFixture<WaistHistoryChartCardComponent> }> {
    await TestBed.configureTestingModule({
        imports: [WaistHistoryChartCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(WaistHistoryChartCardComponent);
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.componentRef.setInput('chartData', chartData);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WaistHistoryChartCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
