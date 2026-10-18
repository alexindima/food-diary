import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

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
const DENSE_LABEL_SPACING = 20;
const ROOMY_LABEL_SPACING = 60;
const MEASURED_LABEL_WIDTH = 30;
const RESIZE_CATEGORY_COUNT = 7;
const FIRST_RESIZE_DAY = 11;
const DENSE_VISIBLE_LABELS = 4;

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
        expect(host().querySelector('.fd-ui-bar-chart__categorical-baseline')).not.toBeNull();
        expect(getText('.fd-ui-bar-chart__axis-unit')).toBe('kcal');
        expect(getText('.fd-ui-bar-chart__axis-label')).toBe('2500');
        expect(getText('.fd-ui-bar-chart__axis-sizer')).toContain('2500');
        expect(getText('.fd-ui-bar-chart__reference-line')).toBe('Goal 2,258');
        expect(host().querySelector('.fd-ui-bar-chart__reference-line--label-at-top')).toBeNull();
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

describe('FdUiBarChartComponent accessibility', () => {
    it('thins overlapping labels, restores them on resize, and retains every accessible bar', async () => {
        let spacing = DENSE_LABEL_SPACING;
        const observers: Array<() => void> = [];
        vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(function (this: HTMLElement): DOMRect {
            if (this.parentElement?.classList.contains('fd-ui-bar-chart__categorical-labels') === true) {
                const index = Array.from(this.parentElement.children).indexOf(this);
                return new DOMRect(index * spacing, 0, MEASURED_LABEL_WIDTH, DENSE_LABEL_SPACING);
            }
            return new DOMRect();
        });
        vi.stubGlobal(
            'ResizeObserver',
            class {
                public constructor(callback: () => void) {
                    observers.push(callback);
                }
                public observe(): void {}
                public disconnect(): void {}
            },
        );

        try {
            await TestBed.configureTestingModule({ imports: [FdUiBarChartComponent] }).compileComponents();
            const fixture = TestBed.createComponent(FdUiBarChartComponent);
            const host = fixture.nativeElement as HTMLElement;
            fixture.componentRef.setInput(
                'categories',
                Array.from({ length: RESIZE_CATEGORY_COUNT }, (_, index) => ({
                    label: `Oct ${index + FIRST_RESIZE_DAY}`,
                    values: [{ label: 'Calories', value: index + 1 }],
                })),
            );
            fixture.detectChanges();
            await fixture.whenStable();
            fixture.detectChanges();
            const labels = (): HTMLElement[] => Array.from(host.querySelectorAll('.fd-ui-bar-chart__categorical-labels span'));
            const visible = (): HTMLElement[] => labels().filter(label => label.style.visibility !== 'hidden');
            expect(visible()).toHaveLength(DENSE_VISIBLE_LABELS);
            expect(visible().at(0)?.textContent.trim()).toBe('Oct 11');
            expect(visible().at(-1)?.textContent.trim()).toBe('Oct 17');

            spacing = ROOMY_LABEL_SPACING;
            observers.at(-1)?.();
            fixture.detectChanges();
            await fixture.whenStable();
            expect(visible()).toHaveLength(RESIZE_CATEGORY_COUNT);

            spacing = DENSE_LABEL_SPACING;
            observers.at(-1)?.();
            fixture.detectChanges();
            await fixture.whenStable();
            expect(visible()).toHaveLength(DENSE_VISIBLE_LABELS);
            expect(host.querySelectorAll('.fd-ui-bar-chart__categorical-bar[role="img"]')).toHaveLength(RESIZE_CATEGORY_COUNT);
            expect(host.querySelector('.fd-ui-bar-chart__category:last-child [role="img"]')?.getAttribute('aria-label')).toContain(
                'Oct 17',
            );
            fixture.destroy();
        } finally {
            vi.restoreAllMocks();
            vi.unstubAllGlobals();
        }
    });

    it.each(['single', 'grouped', 'stacked'] as const)(
        'exposes focusable categorical bars to assistive technology in %s layout',
        async layout => {
            await TestBed.configureTestingModule({ imports: [FdUiBarChartComponent] }).compileComponents();
            const fixture = TestBed.createComponent(FdUiBarChartComponent);
            const host = fixture.nativeElement as HTMLElement;
            fixture.componentRef.setInput('layout', layout);
            fixture.componentRef.setInput('categories', [{ label: 'Monday', values: [{ label: 'Calories', value: PROTEIN_VALUE }] }]);
            fixture.detectChanges();

            const bar = host.querySelector<HTMLElement>('.fd-ui-bar-chart__categorical-bar');
            expect(bar).not.toBeNull();
            expect(bar?.getAttribute('role')).toBe('img');
            expect(bar?.getAttribute('aria-label')).toContain('Monday');
            expect(bar?.tabIndex).toBe(0);
            expect(bar?.closest('[aria-hidden="true"]')).toBeNull();
            bar?.focus();
            expect(document.activeElement).toBe(bar);
        },
    );
});
