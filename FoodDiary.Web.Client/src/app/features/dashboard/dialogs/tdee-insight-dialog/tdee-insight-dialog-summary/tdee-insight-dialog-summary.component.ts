import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { TdeeInsight } from '../../../models/tdee-insight.data';

@Component({
    selector: 'fd-tdee-insight-dialog-summary',
    imports: [DecimalPipe, FdUiIconComponent, TranslatePipe],
    templateUrl: './tdee-insight-dialog-summary.component.html',
    styleUrl: '../tdee-insight-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogSummaryComponent {
    public readonly insight = input.required<TdeeInsight | null>();
    public readonly stateKey = input.required<string>();
    public readonly effectiveTdee = input.required<number | null>();
    public readonly summaryKey = input.required<string>();
    public readonly confidenceKey = input.required<string>();
}
