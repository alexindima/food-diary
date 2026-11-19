import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiLineChartXAxisComponent, type FdUiLineChartXAxisLabelView } from './fd-ui-line-chart-x-axis';

const LABELS: readonly FdUiLineChartXAxisLabelView[] = [
    { label: 'Mon', xPercent: '0%', position: 'start' },
    { label: 'Wed', xPercent: '50%', position: 'middle' },
    { label: 'Fri', xPercent: '100%', position: 'end' },
];
const CROWDED_SPACING = 60;
const ROOMY_SPACING = 100;
const SHORT_HEIGHT = 18;
const WRAPPED_HEIGHT = 36;
const LABEL_WIDTH = 80;
const AXIS_TOP = 100;
const AXIS_WIDTH = 360;

type AxisFixture = ComponentFixture<FdUiLineChartXAxisComponent>;

class AxisObserver {
    public static readonly instances: AxisObserver[] = [];
    public readonly observe = vi.fn<(target: Element) => void>();
    public readonly disconnect = vi.fn<() => void>();

    public constructor(private readonly update: () => void) {
        AxisObserver.instances.push(this);
    }

    public resize(): void {
        this.update();
    }
}

function mockLabelSpacing(spacing: { value: number }): void {
    vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(function (this: HTMLElement): DOMRect {
        if (this.classList.contains('fd-ui-line-chart__x-axis')) {
            return new DOMRect(0, AXIS_TOP, AXIS_WIDTH, SHORT_HEIGHT);
        }
        const parent = this.parentElement;
        if (this.tagName === 'SPAN' && parent?.classList.contains('fd-ui-line-chart__x-axis') === true) {
            const index = [...parent.children].indexOf(this);
            const height = this.textContent.trim() === 'Wed' ? WRAPPED_HEIGHT : SHORT_HEIGHT;
            return new DOMRect(index * spacing.value, AXIS_TOP, LABEL_WIDTH, height);
        }
        return new DOMRect();
    });
}

function axis(fixture: AxisFixture): HTMLElement {
    const element = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.fd-ui-line-chart__x-axis');
    if (element === null) {
        throw new Error('The visible x-axis was not rendered');
    }
    return element;
}

function visibleLabels(element: HTMLElement): string[] {
    return [...element.querySelectorAll('span')]
        .filter(label => label.style.visibility !== 'hidden')
        .map(label => label.textContent.trim());
}

async function updateAsync(fixture: AxisFixture): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
}

function currentObserver(): AxisObserver {
    const observer = AxisObserver.instances.at(-1);
    if (observer === undefined) {
        throw new Error('The chart did not observe the visible axis');
    }
    return observer;
}

beforeEach(() => {
    AxisObserver.instances.length = 0;
    TestBed.configureTestingModule({ imports: [FdUiLineChartXAxisComponent] });
});

afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

describe('Line chart x-axis measurement', () => {
    it('restores an overlapping label on resize, grows with its new height and disconnects when hidden', async () => {
        vi.stubGlobal('ResizeObserver', AxisObserver);
        const spacing = { value: CROWDED_SPACING };
        mockLabelSpacing(spacing);
        const fixture = TestBed.createComponent(FdUiLineChartXAxisComponent);
        fixture.componentRef.setInput('labels', LABELS);
        fixture.componentRef.setInput('show', true);
        await updateAsync(fixture);

        const element = axis(fixture);
        const observer = currentObserver();
        expect(observer.observe.mock.calls).toEqual([element, ...element.querySelectorAll('span')].map(target => [target]));
        expect(visibleLabels(element)).toEqual(['Mon', 'Fri']);
        expect(element.style.minHeight).toContain('22px');

        spacing.value = ROOMY_SPACING;
        observer.resize();
        await updateAsync(fixture);
        expect(visibleLabels(element)).toEqual(['Mon', 'Wed', 'Fri']);
        expect(element.style.minHeight).toContain('40px');

        const active = currentObserver();
        fixture.componentRef.setInput('show', false);
        await updateAsync(fixture);
        expect(element.isConnected).toBe(false);
        expect(active.disconnect).toHaveBeenCalledOnce();
    });

    it('measures the axis on input changes even when ResizeObserver is unavailable', async () => {
        vi.stubGlobal('ResizeObserver', undefined);
        const spacing = { value: CROWDED_SPACING };
        mockLabelSpacing(spacing);
        const fixture = TestBed.createComponent(FdUiLineChartXAxisComponent);
        fixture.componentRef.setInput('labels', LABELS);
        fixture.componentRef.setInput('show', true);
        await updateAsync(fixture);
        expect(visibleLabels(axis(fixture))).toEqual(['Mon', 'Fri']);

        spacing.value = ROOMY_SPACING;
        fixture.componentRef.setInput('layout', 'stacked');
        await updateAsync(fixture);
        const element = axis(fixture);
        expect(visibleLabels(element)).toEqual(['Mon', 'Wed', 'Fri']);
        expect(element.style.minHeight).toContain('--fd-line-chart-stacked-axis-height');
    });
});
