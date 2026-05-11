import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { TdeeInsight } from '../../models/tdee-insight.data';
import { TdeeInsightCardDetailsComponent } from './tdee-insight-card-details.component';

@Component({
    selector: 'fd-tdee-insight-card-content',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiIconComponent, TdeeInsightCardDetailsComponent, TranslatePipe],
    templateUrl: './tdee-insight-card-content.component.html',
    styleUrl: './tdee-insight-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightCardContentComponent {
    public readonly insight = input.required<TdeeInsight | null>();
    public readonly effectiveTdee = input.required<number>();
    public readonly confidenceLabel = input.required<string | null>();
    public readonly confidenceClass = input.required<string>();
    public readonly weightTrendFormatted = input.required<string | null>();
    public readonly hintKey = input.required<string | null>();
    public readonly showSuggestion = input.required<boolean>();

    public readonly applyGoal = output<Event>();
}
