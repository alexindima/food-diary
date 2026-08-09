import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const PERCENT_MAX = 100;

@Component({
    selector: 'fd-ui-progress-ring',
    templateUrl: './fd-ui-progress-ring.html',
    styleUrl: './fd-ui-progress-ring.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiProgressRingComponent {
    public readonly value = input.required<number>();
    public readonly max = input(PERCENT_MAX);
    public readonly ariaLabel = input.required<string>();
    public readonly ariaDescribedBy = input<string>();
    public readonly focusable = input(false);

    protected readonly ariaValue = computed(() => {
        const max = this.max();
        if (!Number.isFinite(max) || max <= 0) {
            return 0;
        }

        const value = Number.isFinite(this.value()) ? this.value() : 0;
        return Math.min(Math.max(value, 0), max);
    });

    protected readonly normalizedValue = computed(() => {
        const max = this.max();
        if (!Number.isFinite(max) || max <= 0) {
            return 0;
        }

        const value = Number.isFinite(this.value()) ? this.value() : 0;
        return Math.min(PERCENT_MAX, Math.max(0, (value / max) * PERCENT_MAX));
    });
}
