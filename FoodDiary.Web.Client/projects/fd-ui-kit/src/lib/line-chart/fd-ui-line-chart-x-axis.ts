import { afterRenderEffect, ChangeDetectionStrategy, Component, computed, type ElementRef, input, signal, viewChild } from '@angular/core';

import type { FdUiLineChartXAxisLabelLayout } from './fd-ui-line-chart.types';
import { findOverlappingXAxisLabels } from './fd-ui-line-chart-x-axis-layout';

const LABEL_CLEARANCE_PX = 4;

export type FdUiLineChartXAxisLabelView = {
    label: string;
    xPercent: string;
    position: 'start' | 'middle' | 'end';
};

@Component({
    selector: 'fd-ui-line-chart-x-axis',
    imports: [],
    templateUrl: './fd-ui-line-chart-x-axis.html',
    styleUrl: './fd-ui-line-chart-x-axis.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLineChartXAxisComponent {
    public readonly show = input(false);
    public readonly labels = input<readonly FdUiLineChartXAxisLabelView[]>([]);
    public readonly layout = input<FdUiLineChartXAxisLabelLayout>('angled');

    protected readonly stackedLineCount = computed(() => Math.max(1, ...this.labels().map(label => label.label.split('\n').length)));
    protected readonly hiddenLabelIndexes = signal<ReadonlySet<number>>(new Set());
    private readonly measuredAxisHeight = signal<number | null>(null);
    protected readonly axisMinHeight = computed(() => {
        const height = this.measuredAxisHeight();
        if (height === null) {
            return null;
        }
        const baseline = this.layout() === 'angled' ? '--fd-line-chart-angled-axis-height' : '--fd-line-chart-stacked-axis-height';
        return `max(var(${baseline}), ${height}px)`;
    });
    private readonly axis = viewChild<ElementRef<HTMLElement>>('axis');

    public constructor() {
        afterRenderEffect(onCleanup => {
            this.labels();
            this.layout();
            const axis = this.axis()?.nativeElement;
            if (axis === undefined) {
                return;
            }

            const labels = Array.from(axis.querySelectorAll('span'));
            const updateLayout = (): void => {
                const bounds = labels.flatMap((label, index) => {
                    const rect = label.getBoundingClientRect();
                    return rect.width > 0 ? [{ index, left: rect.left, right: rect.right, bottom: rect.bottom }] : [];
                });
                const hidden = findOverlappingXAxisLabels(bounds, LABEL_CLEARANCE_PX);
                const previous = this.hiddenLabelIndexes();
                if (previous.size !== hidden.size || [...hidden].some(index => !previous.has(index))) {
                    this.hiddenLabelIndexes.set(hidden);
                }
                const visibleBounds = bounds.filter(label => !hidden.has(label.index));
                this.measuredAxisHeight.set(
                    visibleBounds.length > 0
                        ? Math.ceil(
                              Math.max(...visibleBounds.map(label => label.bottom)) - axis.getBoundingClientRect().top + LABEL_CLEARANCE_PX,
                          )
                        : null,
                );
            };
            updateLayout();

            if (typeof ResizeObserver !== 'undefined') {
                const observer = new ResizeObserver(updateLayout);
                observer.observe(axis);
                labels.forEach(label => {
                    observer.observe(label);
                });
                onCleanup(() => {
                    observer.disconnect();
                });
            }
        });
    }
}
