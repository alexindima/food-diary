import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiLineChartComponent } from './fd-ui-line-chart.component';

const TREND_POINT_COUNT = 3;
const CHART_TOP_Y = 6;
const CHART_BOTTOM_Y = 58;
const EXPLICIT_MAX_VALUE = 5;
const GRID_LINE_COUNT = 3;
const DEFAULT_MAX_VALUE = 100;

describe('FdUiLineChartComponent', () => {
    let component: FdUiLineChartComponent;
    let fixture: ComponentFixture<FdUiLineChartComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const getText = (selector: string): string => getHostText(host(), selector);

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiLineChartComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiLineChartComponent);
        component = fixture.componentInstance;
    });

    it('renders line path and point titles', () => {
        fixture.componentRef.setInput('title', 'Weight');
        fixture.componentRef.setInput('points', [
            { label: 'Mon', value: 80 },
            { label: 'Tue', value: 79 },
            { label: 'Wed', value: 81 },
        ]);
        fixture.detectChanges();

        expect(component.pointViews()).toHaveLength(TREND_POINT_COUNT);
        expect(component.linePath()).toContain('M');
        expect(component.linePath()).toContain('L');
        expect(host().querySelectorAll('.fd-ui-line-chart__point')).toHaveLength(TREND_POINT_COUNT);
        expect(getText('.fd-ui-line-chart__point title')).toBe('Mon: 80');
        expect(component.ariaLabel()).toBe('Weight: Mon 80, Tue 79, Wed 81');
    });

    it('can render an area path', () => {
        fixture.componentRef.setInput('showArea', true);
        fixture.componentRef.setInput('points', [
            { label: 'One', value: 1 },
            { label: 'Two', value: 2 },
        ]);
        fixture.detectChanges();

        expect(component.areaPath()).toContain('Z');
        expect(host().querySelector('.fd-ui-line-chart__area')).not.toBeNull();
    });

    it('can use an explicit value range', () => {
        fixture.componentRef.setInput('points', [{ label: 'One', value: 3 }]);
        fixture.componentRef.setInput('minValue', 1);
        fixture.componentRef.setInput('maxValue', EXPLICIT_MAX_VALUE);
        fixture.detectChanges();

        expect(component.pointViews()[0]?.y).toBe((CHART_TOP_Y + CHART_BOTTOM_Y) / 2);
    });

    it('can render axis labels', () => {
        fixture.componentRef.setInput('points', [
            { label: 'Mon', value: 80 },
            { label: 'Wed', value: 81.5 },
        ]);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.componentRef.setInput('valueSuffix', 'kg');
        fixture.detectChanges();

        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.textContent).toContain('Mon');
        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.textContent).toContain('Wed');
        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).toContain('81.5 kg');
        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).toContain('80 kg');
    });

    it('can render grid lines and use a default max for flat zero data', () => {
        fixture.componentRef.setInput('points', [
            { label: 'Mon', value: 0 },
            { label: 'Tue', value: 0 },
        ]);
        fixture.componentRef.setInput('showGrid', true);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.componentRef.setInput('defaultMaxValue', DEFAULT_MAX_VALUE);
        fixture.detectChanges();

        expect(host().querySelectorAll('.fd-ui-line-chart__grid-line')).toHaveLength(GRID_LINE_COUNT);
        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).toContain('100');
        expect(component.pointViews().every(point => point.y === CHART_BOTTOM_Y)).toBe(true);
    });

    it('shows empty state without numeric values', () => {
        fixture.componentRef.setInput('points', [{ label: 'Mon', value: null }]);
        fixture.detectChanges();

        expect(component.pointViews()).toHaveLength(0);
        expect(getText('.fd-ui-line-chart__empty')).toBe('No data');
    });
});

function getHostText(host: HTMLElement, selector: string): string {
    const element = host.querySelector(selector);
    if (element === null) {
        throw new Error(`Expected ${selector} to exist`);
    }

    return element.textContent.trim();
}
