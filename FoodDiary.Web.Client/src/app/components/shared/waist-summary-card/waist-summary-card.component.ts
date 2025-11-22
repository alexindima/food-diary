import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'fd-waist-summary-card',
    standalone: true,
    imports: [FdUiCardComponent, TranslatePipe, DecimalPipe],
    templateUrl: './waist-summary-card.component.html',
    styleUrls: ['./waist-summary-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistSummaryCardComponent {
    public readonly latest = input<number | null>(null);
    public readonly previous = input<number | null>(null);
    public readonly desired = input<number | null>(null);
    public readonly cardClick = output<void>();

    private readonly translateService = inject(TranslateService);

    public readonly metaText = computed(() => {
        const desiredValue = this.desired();
        if (desiredValue !== null && desiredValue !== undefined) {
            return this.translateService.instant('WAIST_CARD.GOAL', { value: desiredValue });
        }

        return this.translateService.instant('WAIST_CARD.META_EMPTY');
    });

    public readonly trend = computed(() => {
        const latest = this.latest();
        const previous = this.previous();
        if (!latest || !previous) {
            return {
                label: this.translateService.instant('WAIST_CARD.NO_PREVIOUS'),
                status: 'neutral' as TrendStatus,
            };
        }

        const diff = latest - previous;
        if (Math.abs(diff) < 0.01) {
            return {
                label: this.translateService.instant('WAIST_CARD.NO_CHANGE'),
                status: 'neutral' as TrendStatus,
            };
        }

        const direction = diff > 0 ? '↗' : '↘';
        const desired = this.desired();

        let status: TrendStatus = 'neutral';
        if (desired !== null && desired !== undefined) {
            const isImproving =
                (diff < 0 && latest > desired) ||
                (diff > 0 && latest < desired);
            status = isImproving ? 'positive' : 'negative';
        }

        return {
            label: `${direction} ${Math.abs(diff).toFixed(1)} ${this.translateService.instant('WAIST_CARD.CM')}`,
            status,
        };
    });
}

type TrendStatus = 'positive' | 'negative' | 'neutral';
