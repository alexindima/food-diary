import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { TdeeInsight } from '../../models/tdee-insight.data';

@Component({
    selector: 'fd-tdee-insight-card-details',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './tdee-insight-card-details.component.html',
    styleUrl: './tdee-insight-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightCardDetailsComponent {
    public readonly insight = input.required<TdeeInsight>();
    public readonly weightTrendFormatted = input.required<string | null>();
}
