import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { NutrientsSummaryChartsComponent } from './nutrients-summary-charts';

const CHART_BLOCK_SIZE = 100;

async function setupNutrientsSummaryChartsAsync(): Promise<ComponentFixture<NutrientsSummaryChartsComponent>> {
    await TestBed.configureTestingModule({
        imports: [NutrientsSummaryChartsComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(NutrientsSummaryChartsComponent);
    fixture.componentRef.setInput('showPieChart', true);
    fixture.componentRef.setInput('showBarChart', false);
    fixture.componentRef.setInput('isColumnLayout', false);
    fixture.componentRef.setInput('chartsBlockSize', CHART_BLOCK_SIZE);
    fixture.componentRef.setInput('chartsWrapperStyles', { gap: '16px' });
    fixture.componentRef.setInput('chartStyles', { width: '100px', height: '100px' });
    fixture.componentRef.setInput('pieSegments', [{ label: 'Protein', value: 1 }]);
    fixture.componentRef.setInput('barItems', [{ label: 'Protein', value: 1 }]);
    return fixture;
}

describe('NutrientsSummaryChartsComponent', () => {
    it('renders configured chart wrapper', async () => {
        const fixture = await setupNutrientsSummaryChartsAsync();
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('.charts-wrapper')).toBeTruthy();
    });
});
