import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiPieChartComponent } from './fd-ui-pie-chart';

const CHART_TOTAL = 10;
const SEGMENT_COUNT = 2;

describe('FdUiPieChartComponent', () => {
    let component: FdUiPieChartComponent;
    let fixture: ComponentFixture<FdUiPieChartComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiPieChartComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiPieChartComponent);
        component = fixture.componentInstance;
    });

    it('should render chart segments and total', () => {
        fixture.componentRef.setInput('title', 'Browsers');
        fixture.componentRef.setInput('segments', [
            { label: 'Opera', value: 8 },
            { label: 'Chrome', value: 2 },
        ]);
        fixture.detectChanges();

        expect(component['total']()).toBe(CHART_TOTAL);
        expect(host().querySelectorAll('.fd-ui-pie-chart__segment')).toHaveLength(SEGMENT_COUNT);
        expect(host().querySelector('.fd-ui-pie-chart__center strong')?.textContent.trim()).toBe('Browsers');
        expect(host().querySelector('.fd-ui-pie-chart__segment title')?.textContent.trim()).toBe('Opera: 8');
        expect(host().querySelector('.fd-ui-pie-chart__legend-item')?.getAttribute('title')).toBe('Opera: 8');
        expect(component['ariaLabel']()).toContain('Browsers: Opera 8, Chrome 2');
    });

    it('should show empty state when there are no positive values', () => {
        fixture.componentRef.setInput('segments', [{ label: 'Desktop', value: 0 }]);
        fixture.detectChanges();

        expect(component['total']()).toBe(0);
        expect(host().querySelectorAll('.fd-ui-pie-chart__segment')).toHaveLength(0);
        expect(host().querySelector('.fd-ui-pie-chart__empty')?.textContent.trim()).toBe('No data');
    });

    it('should hide legend when requested', () => {
        fixture.componentRef.setInput('segments', [{ label: 'Opera', value: 8 }]);
        fixture.componentRef.setInput('showLegend', false);
        fixture.detectChanges();

        expect(host().querySelector('.fd-ui-pie-chart__legend')).toBeNull();
        expect(host().querySelector('.fd-ui-pie-chart__empty')).toBeNull();
    });

    it('should render filled pie variant without center label', () => {
        fixture.componentRef.setInput('title', 'Browsers');
        fixture.componentRef.setInput('variant', 'pie');
        fixture.componentRef.setInput('segments', [
            { label: 'Opera', value: 8 },
            { label: 'Chrome', value: 2 },
        ]);
        fixture.detectChanges();

        expect(host().querySelector('.fd-ui-pie-chart')?.classList.contains('fd-ui-pie-chart--pie')).toBe(true);
        expect(host().querySelector('.fd-ui-pie-chart__center')).toBeNull();
        expect(host().querySelector('.fd-ui-pie-chart__segment')?.getAttribute('r')).toBe('7.95775');
        expect(component['ariaLabel']()).toContain('Browsers: Opera 8, Chrome 2');
    });

    it('keeps the title in the empty-state name and gives an unlabeled segment a readable fallback', () => {
        fixture.componentRef.setInput('title', 'Browsers');
        fixture.componentRef.setInput('emptyLabel', 'Unclassified');
        fixture.componentRef.setInput('segments', [{ label: 'Invalid', value: NaN }]);
        fixture.detectChanges();

        const chart = host().querySelector('[role="img"]');
        expect(chart?.getAttribute('aria-label')).toBe('Browsers: Unclassified');
        expect(host().querySelector('.fd-ui-pie-chart__empty')?.textContent.trim()).toBe('Unclassified');
        expect(host().querySelector('.fd-ui-pie-chart__segment')).toBeNull();

        fixture.componentRef.setInput('segments', [
            { label: '  ', value: 1 },
            { label: 'Invalid', value: Infinity },
        ]);
        fixture.detectChanges();
        expect(chart?.getAttribute('aria-label')).toBe('Browsers: Unclassified 1');
        expect(host().querySelector('.fd-ui-pie-chart__legend-label')?.textContent.trim()).toBe('Unclassified');
        expect(host().querySelector('circle.fd-ui-pie-chart__segment title')?.textContent).toBe('Unclassified: 1');
        expect(host().querySelectorAll('circle.fd-ui-pie-chart__segment')).toHaveLength(1);
    });
});
