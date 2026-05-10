import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

const TREND_EPSILON = 0.01;
const TREND_DISPLAY_FRACTION_DIGITS = 1;

@Component({
    selector: 'fd-weight-summary-card',
    standalone: true,
    imports: [FdUiCardComponent, TranslatePipe, DecimalPipe],
    templateUrl: './weight-summary-card.component.html',
    styleUrls: ['./weight-summary-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightSummaryCardComponent {
    public readonly latest = input<number | null>(null);
    public readonly previous = input<number | null>(null);
    public readonly desired = input<number | null>(null);
    public readonly cardClick = output<void>();

    private readonly translateService = inject(TranslateService);

    public readonly metaText = computed(() => {
        const desiredValue = this.desired();
        if (desiredValue !== null) {
            return this.translateService.instant('WEIGHT_CARD.GOAL', { value: desiredValue });
        }

        return this.translateService.instant('WEIGHT_CARD.META_EMPTY');
    });

    public readonly trend = computed(() => {
        const latest = this.latest();
        const previous = this.previous();
        if (latest === null || previous === null) {
            return {
                label: this.translateService.instant('WEIGHT_CARD.NO_PREVIOUS'),
                status: 'neutral',
            };
        }

        const diff = latest - previous;
        if (Math.abs(diff) < TREND_EPSILON) {
            return {
                label: this.translateService.instant('WEIGHT_CARD.NO_CHANGE'),
                status: 'neutral',
            };
        }

        const direction = diff > 0 ? '↗' : '↘';
        const desired = this.desired();

        let status: TrendStatus = 'neutral';
        if (desired !== null) {
            const isImproving = (diff < 0 && latest > desired) || (diff > 0 && latest < desired);
            status = isImproving ? 'positive' : 'negative';
        }

        return {
            label: `${direction} ${Math.abs(diff).toFixed(TREND_DISPLAY_FRACTION_DIGITS)} ${this.translateService.instant('WEIGHT_CARD.KG')}`,
            status,
        };
    });
}

type TrendStatus = 'positive' | 'negative' | 'neutral';
