import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import {
    calculateMaxPosition,
    calculateProgressBarWidth,
    calculateProgressColor,
    calculateProgressPercent,
    calculateTextPosition,
    resolveProgressTextColorClass,
} from './dynamic-progress-bar.utils';

@Component({
    selector: 'fd-dynamic-progress-bar',
    templateUrl: './dynamic-progress-bar.component.html',
    styleUrls: ['./dynamic-progress-bar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DynamicProgressBarComponent {
    public readonly current = input.required<number>();
    public readonly max = input.required<number>();
    public readonly unit = input<string>('');

    protected readonly progress = computed<number>(() => calculateProgressPercent(this.current(), this.max()));

    protected readonly progressBarWidth = computed<string>(() => calculateProgressBarWidth(this.progress()));

    protected readonly maxPosition = computed(() => calculateMaxPosition(this.current(), this.max()));
    protected readonly maxPositionOffset = computed<string>(() => `${this.maxPosition()}%`);

    protected readonly textPosition = computed<string>(() =>
        calculateTextPosition(this.progress(), this.current(), this.max(), this.maxPosition()),
    );
    protected readonly barColor = computed(() => calculateProgressColor(this.progress()));
    protected readonly textColorClass = computed(() => resolveProgressTextColorClass(this.progress()));
}
