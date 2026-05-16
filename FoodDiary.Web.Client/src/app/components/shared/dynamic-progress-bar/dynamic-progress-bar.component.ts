import { NgStyle } from '@angular/common';
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
    imports: [NgStyle],
    templateUrl: './dynamic-progress-bar.component.html',
    styleUrls: ['./dynamic-progress-bar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DynamicProgressBarComponent {
    public readonly current = input.required<number>();
    public readonly max = input.required<number>();
    public readonly unit = input<string>('');

    public readonly progress = computed<number>(() => calculateProgressPercent(this.current(), this.max()));

    public readonly progressBarWidth = computed<string>(() => calculateProgressBarWidth(this.progress()));

    public readonly maxPosition = computed(() => calculateMaxPosition(this.current(), this.max()));
    public readonly maxPositionOffset = computed<string>(() => `${this.maxPosition()}%`);

    public readonly textPosition = computed<string>(() =>
        calculateTextPosition(this.progress(), this.current(), this.max(), this.maxPosition()),
    );
    public readonly barColor = computed(() => calculateProgressColor(this.progress()));
    public readonly textColorClass = computed(() => resolveProgressTextColorClass(this.progress()));
}
