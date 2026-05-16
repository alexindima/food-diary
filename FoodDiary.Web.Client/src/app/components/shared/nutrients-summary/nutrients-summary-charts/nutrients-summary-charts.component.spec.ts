import { type ComponentFixture, TestBed } from '@angular/core/testing';
import type { ChartData, ChartOptions } from 'chart.js';
import { describe, expect, it } from 'vitest';

import { NutrientsSummaryChartsComponent } from './nutrients-summary-charts.component';

const CHART_BLOCK_SIZE = 100;

async function setupNutrientsSummaryChartsAsync(): Promise<ComponentFixture<NutrientsSummaryChartsComponent>> {
    await TestBed.configureTestingModule({
        imports: [NutrientsSummaryChartsComponent],
    }).compileComponents();

    const pieChartData: ChartData<'pie', number[], string> = { labels: ['Protein'], datasets: [{ data: [1] }] };
    const barChartData: ChartData<'bar', number[], string> = { labels: ['Protein'], datasets: [{ data: [1] }] };
    const fixture = TestBed.createComponent(NutrientsSummaryChartsComponent);
    fixture.componentRef.setInput('showPieChart', true);
    fixture.componentRef.setInput('showBarChart', false);
    fixture.componentRef.setInput('isColumnLayout', false);
    fixture.componentRef.setInput('chartsBlockSize', CHART_BLOCK_SIZE);
    fixture.componentRef.setInput('chartsWrapperStyles', { gap: '16px' });
    fixture.componentRef.setInput('chartStyles', { width: '100px', height: '100px' });
    fixture.componentRef.setInput('chartCanvasStyles', { maxWidth: '100px', maxHeight: '100px' });
    fixture.componentRef.setInput('pieChartData', pieChartData);
    fixture.componentRef.setInput('barChartData', barChartData);
    fixture.componentRef.setInput('pieChartOptions', {} satisfies ChartOptions<'pie'>);
    fixture.componentRef.setInput('barChartOptions', {} satisfies ChartOptions<'bar'>);
    return fixture;
}

describe('NutrientsSummaryChartsComponent', () => {
    it('renders configured chart wrapper', async () => {
        const fixture = await setupNutrientsSummaryChartsAsync();
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('.charts-wrapper')).toBeTruthy();
    });
});
