import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { describe, expect, it } from 'vitest';

import { WeightHistoryChartCardComponent } from './weight-history-chart-card.component';

const CHART_VALUE = 74.2;

describe('WeightHistoryChartCardComponent', () => {
    it('derives empty state from chart data labels', async () => {
        const { component, fixture } = await setupComponentAsync({ labels: [], datasets: [] });

        expect(component.hasPoints()).toBe(false);
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.NO_DATA_FOR_CHART');
    });

    it('detects chart points without a separate input', async () => {
        const { component } = await setupComponentAsync(
            {
                labels: ['2026-05-15'],
                datasets: [{ data: [CHART_VALUE], label: 'Weight' }],
            },
            true,
        );

        expect(component.hasPoints()).toBe(true);
    });
});

async function setupComponentAsync(
    chartData: ChartConfiguration<'line'>['data'],
    isLoading = false,
): Promise<{ component: WeightHistoryChartCardComponent; fixture: ComponentFixture<WeightHistoryChartCardComponent> }> {
    await TestBed.configureTestingModule({
        imports: [WeightHistoryChartCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(WeightHistoryChartCardComponent);
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.componentRef.setInput('chartData', chartData);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WeightHistoryChartCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
