import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WaistHistoryChartPoint } from '../../lib/waist-history-chart.mapper';
import { WaistHistoryChartCardComponent } from './waist-history-chart-card.component';

const CHART_VALUE = 82;

describe('WaistHistoryChartCardComponent', () => {
    it('derives empty state from chart data labels', async () => {
        const { component, fixture } = await setupComponentAsync([]);

        expect(component.hasPoints()).toBe(false);
        expect(getText(fixture)).toContain('WAIST_HISTORY.NO_DATA_FOR_CHART');
    });

    it('detects chart points without a separate input', async () => {
        const { component } = await setupComponentAsync([{ label: '2026-05-15', value: CHART_VALUE }], true);

        expect(component.hasPoints()).toBe(true);
    });
});

async function setupComponentAsync(
    chartPoints: readonly WaistHistoryChartPoint[],
    isLoading = false,
): Promise<{ component: WaistHistoryChartCardComponent; fixture: ComponentFixture<WaistHistoryChartCardComponent> }> {
    await TestBed.configureTestingModule({
        imports: [WaistHistoryChartCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(WaistHistoryChartCardComponent);
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.componentRef.setInput('chartPoints', chartPoints);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WaistHistoryChartCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
