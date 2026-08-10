import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

type LevelBar = {
    height: number;
    x: number;
    y: number;
};

const LEVEL_BARS: LevelBar[] = [
    { x: 2, y: 16, height: 6 },
    { x: 9, y: 12, height: 10 },
    { x: 16, y: 7, height: 15 },
    { x: 23, y: 2, height: 20 },
];

@Component({
    selector: 'fd-ui-level-indicator',
    templateUrl: './fd-ui-level-indicator.html',
    styleUrl: './fd-ui-level-indicator.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        'aria-hidden': 'true',
    },
})
export class FdUiLevelIndicatorComponent {
    public readonly filledCount = input(0);
    protected readonly bars = LEVEL_BARS;
    protected readonly normalizedFilledCount = computed(() => Math.min(Math.max(Math.trunc(this.filledCount()), 0), LEVEL_BARS.length));
}
