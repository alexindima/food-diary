import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { TdeeInsight } from '../../../models/tdee-insight.data';

@Component({
    selector: 'fd-tdee-insight-dialog-metrics',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './tdee-insight-dialog-metrics.component.html',
    styleUrl: '../tdee-insight-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogMetricsComponent {
    public readonly insight = input.required<TdeeInsight>();
    public readonly weightTrendFormatted = input.required<string | null>();
}
