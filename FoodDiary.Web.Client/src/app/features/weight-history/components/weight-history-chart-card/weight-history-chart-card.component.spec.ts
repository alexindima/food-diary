import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WeightHistoryChartPoint } from '../../lib/weight-history-chart.mapper';
import { WeightHistoryChartCardComponent } from './weight-history-chart-card.component';

const CHART_VALUE = 74.2;

describe('WeightHistoryChartCardComponent', () => {
    it('derives empty state from chart data labels', async () => {
        const { component, fixture } = await setupComponentAsync([]);

        expect(component.hasPoints()).toBe(false);
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.NO_DATA_FOR_CHART');
    });

    it('detects chart points without a separate input', async () => {
        const { component } = await setupComponentAsync([{ label: '2026-05-15', value: CHART_VALUE }], true);

        expect(component.hasPoints()).toBe(true);
    });
});

async function setupComponentAsync(
    chartPoints: readonly WeightHistoryChartPoint[],
    isLoading = false,
): Promise<{ component: WeightHistoryChartCardComponent; fixture: ComponentFixture<WeightHistoryChartCardComponent> }> {
    await TestBed.configureTestingModule({
        imports: [WeightHistoryChartCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(WeightHistoryChartCardComponent);
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.componentRef.setInput('chartPoints', chartPoints);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WeightHistoryChartCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
