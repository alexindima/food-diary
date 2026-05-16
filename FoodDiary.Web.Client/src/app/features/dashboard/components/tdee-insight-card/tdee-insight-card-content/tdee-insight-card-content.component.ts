import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { buildTdeeHintKey, formatTdeeWeightTrend, hasMeaningfulTdeeSuggestion } from '../../../lib/tdee-insight-view.mapper';
import type { TdeeInsight } from '../../../models/tdee-insight.data';
import { TdeeInsightCardDetailsComponent } from '../tdee-insight-card-details/tdee-insight-card-details.component';

@Component({
    selector: 'fd-tdee-insight-card-content',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiIconComponent, TdeeInsightCardDetailsComponent, TranslatePipe],
    templateUrl: './tdee-insight-card-content.component.html',
    styleUrl: '../tdee-insight-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightCardContentComponent {
    public readonly insight = input.required<TdeeInsight | null>();
    public readonly effectiveTdee = input.required<number>();
    public readonly applyGoal = output<Event>();

    public readonly confidenceLabel = computed(() => {
        const insight = this.insight();
        return insight !== null && insight.confidence !== 'none' ? `TDEE_CARD.CONFIDENCE.${insight.confidence.toUpperCase()}` : null;
    });
    public readonly confidenceClass = computed(() => this.insight()?.confidence ?? 'none');
    public readonly weightTrendFormatted = computed(() => formatTdeeWeightTrend(this.insight()?.weightTrendPerWeek));
    public readonly hintKey = computed(() => buildTdeeHintKey(this.insight()?.goalAdjustmentHint));
    public readonly showSuggestion = computed(() => hasMeaningfulTdeeSuggestion(this.insight()));
}
