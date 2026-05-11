import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';
import type { TdeeInsight } from '../../models/tdee-insight.data';
import { TdeeInsightCardContentComponent } from './tdee-insight-card-content.component';

const CALORIE_TARGET_DIFF_THRESHOLD = 50;
const WEIGHT_TREND_FRACTION_DIGITS = 2;

@Component({
    selector: 'fd-tdee-insight-card',
    standalone: true,
    imports: [FdUiIconComponent, TranslatePipe, DashboardWidgetFrameComponent, TdeeInsightCardContentComponent],
    templateUrl: './tdee-insight-card.component.html',
    styleUrl: './tdee-insight-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightCardComponent {
    public readonly insight = input.required<TdeeInsight | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly applyGoal = output<number>();

    public readonly effectiveTdee = computed(() => {
        const data = this.insight();
        return data?.adaptiveTdee ?? data?.estimatedTdee ?? null;
    });

    public readonly confidenceLabel = computed(() => {
        const data = this.insight();
        if (data === null || data.confidence === 'none') {
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
        return `${sign}${trend.toFixed(WEIGHT_TREND_FRACTION_DIGITS)}`;
    });

    public readonly hintKey = computed(() => {
        const hint = this.insight()?.goalAdjustmentHint ?? '';
        if (hint.length === 0) {
            return null;
        }
        return `TDEE_CARD.HINTS.${hint.replace('hint.', '').toUpperCase()}`;
    });

    public readonly showSuggestion = computed(() => {
        const suggestedCalorieTarget = this.insight()?.suggestedCalorieTarget ?? null;
        const currentCalorieTarget = this.insight()?.currentCalorieTarget ?? null;
        if (suggestedCalorieTarget === null || currentCalorieTarget === null) {
            return false;
        }
        return Math.abs(suggestedCalorieTarget - currentCalorieTarget) > CALORIE_TARGET_DIFF_THRESHOLD;
    });

    public onApplyGoal(event?: Event): void {
        event?.stopPropagation();

        const target = this.insight()?.suggestedCalorieTarget;
        if (target !== null && target !== undefined) {
            this.applyGoal.emit(target);
        }
    }
}
