import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiBarChartComponent } from './fd-ui-bar-chart';

const PROTEIN_VALUE = 50;
const FAT_VALUE = 25;
const BAR_COUNT = 2;
const ZERO_HEIGHT = 0;
const GRID_LINE_COUNT = 5;
const CHART_MAXIMUM = 2500;
const REFERENCE_LINE_VALUE = 2258;
const REFERENCE_LINE_TOP_PERCENT = 9.68;
const THREE_QUARTER_TICK = 1875;
const HALF_TICK = 1250;
const QUARTER_TICK = 625;
const CATEGORICAL_TICKS = [CHART_MAXIMUM, THREE_QUARTER_TICK, HALF_TICK, QUARTER_TICK, 0] as const;

describe('FdUiBarChartComponent', () => {
    let component: FdUiBarChartComponent;
    let fixture: ComponentFixture<FdUiBarChartComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiBarChartComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiBarChartComponent);
        component = fixture.componentInstance;
    });

    it('renders proportional bars and labels', () => {
        fixture.componentRef.setInput('title', 'Macros');
        fixture.componentRef.setInput('items', [
            { label: 'Protein', value: PROTEIN_VALUE },
            { label: 'Fat', value: FAT_VALUE },
        ]);
        fixture.detectChanges();

        const bars = host().querySelectorAll('.fd-ui-bar-chart__bar');
        const firstBar = bars.item(0);
        const secondBar = bars.item(1);

        expect(component['maxValue']()).toBe(PROTEIN_VALUE);
        expect(bars).toHaveLength(BAR_COUNT);
        expect(host().querySelectorAll('.fd-ui-bar-chart__grid-line')).toHaveLength(GRID_LINE_COUNT);
        expect(Number(firstBar.getAttribute('height'))).toBeGreaterThan(Number(secondBar.getAttribute('height')));
        expect(getText('.fd-ui-bar-chart__label')).toBe('Protein');
        expect(component['ariaLabel']()).toBe('Macros: Protein 50, Fat 25');
    });

    it('keeps zero value categories visible', () => {
        fixture.componentRef.setInput('items', [{ label: 'Calories', value: 0 }]);
        fixture.detectChanges();

        expect(component['maxValue']()).toBe(0);
        expect(host().querySelectorAll('.fd-ui-bar-chart__bar')).toHaveLength(1);
        expect(Number(host().querySelector('.fd-ui-bar-chart__bar')?.getAttribute('height'))).toBe(ZERO_HEIGHT);
        expect(getText('.fd-ui-bar-chart__label')).toBe('Calories');
    });

    it('shows empty state without items', () => {
        fixture.componentRef.setInput('items', []);
        fixture.detectChanges();

        expect(getText('.fd-ui-bar-chart__empty')).toBe('No data');
    });

    it('renders categorical stacked bars with a scaled axis and reference line', () => {
        fixture.componentRef.setInput('title', 'Nutrition trend');
        fixture.componentRef.setInput('layout', 'stacked');
        fixture.componentRef.setInput('axisUnit', 'kcal');
        fixture.componentRef.setInput('axisTicks', CATEGORICAL_TICKS);
        fixture.componentRef.setInput('scaleMaximum', CHART_MAXIMUM);
        fixture.componentRef.setInput('categories', [
            {
                label: '3 Aug',
                values: [
                    { label: 'Protein', value: 400, color: 'green' },
                    { label: 'Fat', value: 900, color: 'orange' },
                ],
            },
            { label: '4 Aug', values: [{ label: 'Protein', value: null }] },
        ]);
        fixture.componentRef.setInput('referenceLines', [{ value: REFERENCE_LINE_VALUE, label: 'Goal 2,258' }]);
        fixture.detectChanges();

        expect(host().querySelectorAll('.fd-ui-bar-chart__categorical-labels span')).toHaveLength(2);
        expect(host().querySelectorAll('.fd-ui-bar-chart__categorical-bar')).toHaveLength(1);
        expect(host().querySelectorAll('.fd-ui-bar-chart__categorical-segment')).toHaveLength(2);
        expect(getText('.fd-ui-bar-chart__axis-unit')).toBe('kcal');
        expect(getText('.fd-ui-bar-chart__axis-label')).toBe('2500');
        expect(getText('.fd-ui-bar-chart__reference-line')).toBe('Goal 2,258');
        expect(host().querySelector('.fd-ui-bar-chart--edge-inset-none')).not.toBeNull();
        expect(component['referenceLineViews']()[0]?.top).toBeCloseTo(REFERENCE_LINE_TOP_PERCENT);
    });

    function getText(selector: string): string {
        const element = host().querySelector(selector);
        if (element === null) {
            throw new Error(`Expected ${selector} to exist`);
        }

        return element.textContent.trim();
    }
});
