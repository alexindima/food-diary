import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { TranslatePipe } from '@ngx-translate/core';
import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { TdeeInsight } from '../../models/tdee-insight.data';

@Component({
    selector: 'fd-tdee-insight-card',
    standalone: true,
    imports: [CommonModule, FdUiIconModule, TranslatePipe, FdCardHoverDirective, FdUiButtonComponent],
    templateUrl: './tdee-insight-card.component.html',
    styleUrl: './tdee-insight-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightCardComponent {
    public readonly insight = input<TdeeInsight | null>(null);
    public readonly isLoading = input<boolean>(false);
    public readonly applyGoal = output<number>();

    public readonly effectiveTdee = computed(() => {
        const data = this.insight();
        return data?.adaptiveTdee ?? data?.estimatedTdee ?? null;
    });

    public readonly confidenceLabel = computed(() => {
        const data = this.insight();
        if (!data || data.confidence === 'none') {
            return null;
        }
        return `TDEE_CARD.CONFIDENCE.${data.confidence.toUpperCase()}`;
    });

    public readonly confidenceClass = computed(() => {
        const data = this.insight();
        return data?.confidence ?? 'none';
    });

    public readonly weightTrendFormatted = computed(() => {
        const trend = this.insight()?.weightTrendPerWeek;
        if (trend === null || trend === undefined) {
            return null;
        }
        const sign = trend > 0 ? '+' : '';
        return `${sign}${trend.toFixed(2)}`;
    });

    public readonly hintKey = computed(() => {
        const hint = this.insight()?.goalAdjustmentHint;
        if (!hint) {
            return null;
        }
        return `TDEE_CARD.HINTS.${hint.replace('hint.', '').toUpperCase()}`;
    });

    public readonly showSuggestion = computed(() => {
        const data = this.insight();
        if (!data?.suggestedCalorieTarget || !data.currentCalorieTarget) {
            return false;
        }
        return Math.abs(data.suggestedCalorieTarget - data.currentCalorieTarget) > 50;
    });

    public onApplyGoal(): void {
        const target = this.insight()?.suggestedCalorieTarget;
        if (target) {
            this.applyGoal.emit(target);
        }
    }
}
