import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiLineChartComponent } from './fd-ui-line-chart.component';

const TREND_POINT_COUNT = 3;
const CHART_LEFT_X = 2;
const CHART_RIGHT_X = 98;
const SPARKLINE_CHART_LEFT_X = 0;
const SPARKLINE_CHART_RIGHT_X = 100;
const CHART_TOP_Y = 6;
const CHART_BOTTOM_Y = 58;
const SPARKLINE_AREA_BASELINE_Y = 63.2;
const EXPLICIT_MAX_VALUE = 5;
const GRID_LINE_COUNT = 5;
const GRID_POINT_COUNT = 2;
const LIMITED_LABEL_COUNT = 14;
const MONTH_LABEL_COUNT = 11;
const DEFAULT_MAX_VALUE = 100;
const FLAT_VALUE = 80;
const HIGH_CALORIE_VALUE = 7901;
const SMALL_NUTRIENT_VALUE = 58;
const SMALL_MULTI_SERIES_VALUE = 24;
const MULTI_SERIES_COUNT = 4;
const MULTI_SERIES_POINT_COUNT = 8;

// eslint-disable-next-line max-lines-per-function -- Line chart primitive behaviors are easier to scan in one component suite.
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
        expect(component.linePath()).toContain('C');
        expect(component.pointViews()[0]?.x).toBe(CHART_LEFT_X);
        expect(component.pointViews()[TREND_POINT_COUNT - 1]?.x).toBe(CHART_RIGHT_X);
        expect(host().querySelectorAll('.fd-ui-line-chart__point')).toHaveLength(TREND_POINT_COUNT);
        expect(host().querySelector('.fd-ui-line-chart__point')?.getAttribute('title')).toBe('Mon: 80');
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

    it('uses full horizontal range for sparklines', () => {
        fixture.componentRef.setInput('density', 'sparkline');
        fixture.componentRef.setInput('points', [
            { label: 'One', value: 1 },
            { label: 'Two', value: 2 },
        ]);
        fixture.detectChanges();

        expect(component.pointViews()[0]?.x).toBe(SPARKLINE_CHART_LEFT_X);
        expect(component.pointViews()[1]?.x).toBe(SPARKLINE_CHART_RIGHT_X);
    });

    it('extends sparkline area below the zero line stroke', () => {
        fixture.componentRef.setInput('density', 'sparkline');
        fixture.componentRef.setInput('showArea', true);
        fixture.componentRef.setInput('points', [
            { label: 'One', value: 0 },
            { label: 'Two', value: 2 },
        ]);
        fixture.detectChanges();

        expect(component.areaPath()).toContain(`100 ${SPARKLINE_AREA_BASELINE_Y}`);
        expect(component.areaPath()).toContain(`0 ${SPARKLINE_AREA_BASELINE_Y}`);
    });

    it('can render multiple line series with a shared y-axis', () => {
        fixture.componentRef.setInput('series', [
            {
                label: 'Proteins',
                color: 'blue',
                points: [
                    { label: 'Mon', value: 10 },
                    { label: 'Tue', value: 20 },
                ],
            },
            {
                label: 'Fats',
                color: 'gold',
                points: [
                    { label: 'Mon', value: 5 },
                    { label: 'Tue', value: 15 },
                ],
            },
            {
                label: 'Carbs',
                color: 'green',
                points: [
                    { label: 'Mon', value: 25 },
                    { label: 'Tue', value: 30 },
                ],
            },
            {
                label: 'Fiber',
                color: 'purple',
                points: [
                    { label: 'Mon', value: 2 },
                    { label: 'Tue', value: 4 },
                ],
            },
        ]);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.componentRef.setInput('showGrid', true);
        fixture.detectChanges();

        expect(host().querySelectorAll('.fd-ui-line-chart__line')).toHaveLength(MULTI_SERIES_COUNT);
        expect(host().querySelectorAll('.fd-ui-line-chart__point')).toHaveLength(MULTI_SERIES_POINT_COUNT);
        expect(host().querySelectorAll('.fd-ui-line-chart__legend span')).toHaveLength(MULTI_SERIES_COUNT);
        expect(host().querySelector('.fd-ui-line-chart__legend')?.textContent).toContain('Proteins');
        expect(component.ariaLabel()).toContain('Proteins Mon 10');
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
        expect(host().querySelectorAll('.fd-ui-line-chart__y-axis-label')).toHaveLength(GRID_LINE_COUNT);
        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).toContain('80 kg');
    });

    it('limits x-axis labels while keeping range endpoints', () => {
        const points = Array.from({ length: 15 }, (_, index) => ({ label: `May ${index + 1}`, value: index }));

        fixture.componentRef.setInput('points', points);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.detectChanges();

        const labels = host().querySelectorAll('.fd-ui-line-chart__x-axis span');

        expect(labels).toHaveLength(LIMITED_LABEL_COUNT);
        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.classList.contains('fd-ui-line-chart__x-axis--angled')).toBe(true);
        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.textContent).toContain('May 1');
        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.textContent).toContain('May 15');
    });

    it('shows every x-axis label when the range is within the label limit', () => {
        fixture.componentRef.setInput('points', [
            { label: 'Apr 26', value: 0 },
            { label: 'Apr 29', value: 1 },
            { label: 'May 2', value: 2 },
            { label: 'May 5', value: 3 },
            { label: 'May 8', value: 4 },
            { label: 'May 11', value: 5 },
            { label: 'May 14', value: 6 },
            { label: 'May 17', value: 7 },
            { label: 'May 20', value: 8 },
            { label: 'May 23', value: 9 },
            { label: 'May 26', value: 10 },
        ]);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.detectChanges();

        const labels = host().querySelectorAll('.fd-ui-line-chart__x-axis span');

        expect(labels).toHaveLength(MONTH_LABEL_COUNT);
        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.textContent).toContain('Apr 29');
        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.textContent).toContain('May 8');
        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.textContent).toContain('May 23');
    });

    it('keeps sparse short x-axis labels angled', () => {
        fixture.componentRef.setInput('points', [
            { label: 'A', value: 0 },
            { label: 'B', value: 1 },
            { label: 'C', value: 2 },
        ]);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.detectChanges();

        expect(host().querySelector('.fd-ui-line-chart__x-axis')?.classList.contains('fd-ui-line-chart__x-axis--angled')).toBe(true);
    });

    it('rounds the automatic y-axis maximum to a nice grid value', () => {
        fixture.componentRef.setInput('points', [
            { label: 'May', value: 0 },
            { label: 'June', value: HIGH_CALORIE_VALUE },
        ]);
        fixture.componentRef.setInput('minValue', 0);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.componentRef.setInput('valueSuffix', 'kcal');
        fixture.componentRef.setInput('axisDecimalPlaces', 0);
        fixture.detectChanges();

        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).toContain('8000 kcal');
        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).not.toContain('7901 kcal');
    });

    it('uses a proportionate nice y-axis maximum for small values', () => {
        fixture.componentRef.setInput('points', [
            { label: 'May', value: 0 },
            { label: 'June', value: SMALL_NUTRIENT_VALUE },
        ]);
        fixture.componentRef.setInput('minValue', 0);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.componentRef.setInput('valueSuffix', 'g');
        fixture.componentRef.setInput('axisDecimalPlaces', 0);
        fixture.detectChanges();

        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).toContain('60 g');
    });

    it('does not force default max when non-zero values are present', () => {
        fixture.componentRef.setInput('series', [
            {
                label: 'Carbs',
                points: [
                    { label: 'May', value: 0 },
                    { label: 'June', value: SMALL_MULTI_SERIES_VALUE },
                ],
            },
        ]);
        fixture.componentRef.setInput('minValue', 0);
        fixture.componentRef.setInput('showAxisLabels', true);
        fixture.componentRef.setInput('valueSuffix', 'g');
        fixture.componentRef.setInput('axisDecimalPlaces', 0);
        fixture.componentRef.setInput('defaultMaxValue', DEFAULT_MAX_VALUE);
        fixture.detectChanges();

        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).not.toContain('100 g');
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

        expect(host().querySelectorAll('.fd-ui-line-chart__grid-line')).toHaveLength(GRID_LINE_COUNT + GRID_POINT_COUNT);
        expect(host().querySelectorAll('.fd-ui-line-chart__grid-line[x1][y1="6"][y2="58"]')).toHaveLength(GRID_POINT_COUNT);
        expect(host().querySelector('.fd-ui-line-chart__grid-line[x1="2"][x2="2"]')).not.toBeNull();
        expect(host().querySelector('.fd-ui-line-chart__grid-line[x1="98"][x2="98"]')).not.toBeNull();
        expect(host().querySelector('.fd-ui-line-chart__y-axis')?.textContent).toContain('100');
        expect(component.pointViews().every(point => point.y === CHART_BOTTOM_Y)).toBe(true);
    });

    it('adds visual range around flat non-zero data', () => {
        fixture.componentRef.setInput('points', [
            { label: 'Mon', value: FLAT_VALUE },
            { label: 'Tue', value: FLAT_VALUE },
        ]);
        fixture.detectChanges();

        const yValues = component.pointViews().map(point => point.y);

        expect(yValues.every(y => y > CHART_TOP_Y && y < CHART_BOTTOM_Y)).toBe(true);
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
