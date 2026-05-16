import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { WeightTrendCardComponent, type WeightTrendPoint } from './weight-trend-card.component';

const CURRENT_WEIGHT = 80;
const WEIGHT_CHANGE = -1.24;
const EXPECTED_FORMATTED_CHANGE = '-1.2';
const CHART_MIN_WITH_PADDING = 78.5;

describe('WeightTrendCardComponent', () => {
    it('formats change tone and value', async () => {
        const { component, fixture } = await setupComponentAsync({ change: WEIGHT_CHANGE });

        fixture.detectChanges();

        expect(component.changeTone()).toBe('positive');
        expect(component.formattedChangeValue()).toBe(EXPECTED_FORMATTED_CHANGE);
    });

    it('builds chart data from ordered points and pads y axis', async () => {
        const { component, fixture } = await setupComponentAsync({
            points: [
                { date: '2026-05-03', value: CURRENT_WEIGHT },
                { date: '2026-05-01', value: CURRENT_WEIGHT - 1 },
                { date: '2026-05-02', value: null },
            ],
        });

        fixture.detectChanges();

        const chartData = component.chartData();
        const yScale = component.dynamicChartOptions()?.scales?.['y'];

        expect(chartData?.datasets[0]?.data).toEqual([CURRENT_WEIGHT - 1, null, CURRENT_WEIGHT]);
        expect(yScale).toMatchObject({ min: CHART_MIN_WITH_PADDING });
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
            imports: [WeightTrendCardComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(WeightTrendCardComponent);
    fixture.componentRef.setInput('currentWeight', overrides.currentWeight ?? CURRENT_WEIGHT);
    fixture.componentRef.setInput('change', overrides.change ?? null);
    fixture.componentRef.setInput('timeframeLabel', overrides.timeframeLabel ?? '7 days');
    fixture.componentRef.setInput('points', overrides.points ?? []);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
