import { PLATFORM_ID } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { NutrientsSummaryComponent } from './nutrients-summary';

const CALORIES = 320;
const PROTEINS = 24;
const FATS = 12;
const CARBS = 36;
const FIBER = 5;
const ALCOHOL = 2;
const CUSTOM_GAP = 4;
const WIDE_VIEWPORT = 1000;
const MEDIUM_VIEWPORT = 700;
const NARROW_VIEWPORT = 500;
const MEDIUM_CHART_SIZE = 180;

async function setupNutrientsSummaryAsync(
    platform: 'browser' | 'server' = 'browser',
): Promise<ComponentFixture<NutrientsSummaryComponent>> {
    await TestBed.configureTestingModule({
        imports: [NutrientsSummaryComponent],
        providers: [provideTranslateTesting(), { provide: PLATFORM_ID, useValue: platform }],
    }).compileComponents();

    const fixture = TestBed.createComponent(NutrientsSummaryComponent);
    fixture.componentRef.setInput('calories', CALORIES);
    fixture.componentRef.setInput('nutrientChartData', { proteins: PROTEINS, fats: FATS, carbs: CARBS });
    return fixture;
}

describe('NutrientsSummaryComponent', () => {
    it('renders nutrient values and optional highlights', async () => {
        const fixture = await setupNutrientsSummaryAsync();
        fixture.componentRef.setInput('fiberValue', FIBER);
        fixture.componentRef.setInput('alcoholValue', ALCOHOL);
        fixture.detectChanges();

        const text = (fixture.nativeElement as HTMLElement).textContent;

        expect(text).toContain('SHARED.NUTRIENTS_SUMMARY.FIBER');
        expect(text).toContain('NUTRIENTS.ALCOHOL');
        expect(text).toContain(PROTEINS.toString());
        expect(text).toContain(FATS.toString());
        expect(text).toContain(CARBS.toString());
    });

    it('hides charts when all macro values are empty', async () => {
        const fixture = await setupNutrientsSummaryAsync();
        fixture.componentRef.setInput('nutrientChartData', { proteins: 0, fats: 0, carbs: 0 });
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('fd-nutrients-summary-charts')).toBeNull();
    });

    it('merges custom config into computed styles', async () => {
        const fixture = await setupNutrientsSummaryAsync();
        fixture.componentRef.setInput('config', { styles: { common: { gap: CUSTOM_GAP } } });
        fixture.detectChanges();

        expect(fixture.componentInstance['summaryWrapperStyles']()).toEqual({ gap: `${CUSTOM_GAP}px` });
    });
});

describe('Responsive nutrients summary charts', () => {
    it('resizes real chart containers at both breakpoints and releases the window listener on destruction', async () => {
        const viewport = vi.spyOn(BrowserWindowService.prototype, 'getViewportWidth').mockReturnValue(WIDE_VIEWPORT);
        try {
            const fixture = await setupNutrientsSummaryAsync();
            fixture.componentRef.setInput('bare', true);
            fixture.componentRef.setInput('showBarChart', true);
            fixture.componentRef.setInput('config', { styles: { charts: { breakpoints: { chartBlockSize: MEDIUM_CHART_SIZE } } } });
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            const chartWrapper = host.querySelector<HTMLElement>('.charts-wrapper');
            const charts = [...host.querySelectorAll<HTMLElement>('.charts-wrapper .chart')];
            expect(host.querySelector('fd-custom-group')).toBeNull();
            expect(chartWrapper?.classList.contains('column-layout')).toBe(false);
            expect(charts.map(chart => chart.style.width)).toEqual(['192px', '192px']);

            viewport.mockReturnValue(MEDIUM_VIEWPORT);
            window.dispatchEvent(new Event('resize'));
            fixture.detectChanges();
            expect(chartWrapper?.classList.contains('column-layout')).toBe(true);
            expect(chartWrapper?.style.gap).toBe('12px');
            expect(charts.map(chart => chart.style.width)).toEqual(['180px', '180px']);

            viewport.mockReturnValue(NARROW_VIEWPORT);
            window.dispatchEvent(new Event('resize'));
            fixture.detectChanges();
            expect(charts.map(chart => chart.style.width)).toEqual(['256px', '256px']);

            fixture.destroy();
            viewport.mockClear();
            window.dispatchEvent(new Event('resize'));
            expect(viewport).not.toHaveBeenCalled();
        } finally {
            viewport.mockRestore();
        }
    });

    it('renders readable chart geometry on the server without reading or listening to the browser viewport', async () => {
        const width = vi.spyOn(BrowserWindowService.prototype, 'getViewportWidth');
        const resize = vi.spyOn(BrowserWindowService.prototype, 'onResize');
        try {
            const fixture = await setupNutrientsSummaryAsync('server');
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            expect(host.querySelector<HTMLElement>('.charts-wrapper .chart')?.style.width).toBe('192px');
            expect(host.querySelector('.charts-wrapper.column-layout')).toBeNull();
            expect(width).not.toHaveBeenCalled();
            expect(resize).not.toHaveBeenCalled();
        } finally {
            width.mockRestore();
            resize.mockRestore();
        }
    });
});
