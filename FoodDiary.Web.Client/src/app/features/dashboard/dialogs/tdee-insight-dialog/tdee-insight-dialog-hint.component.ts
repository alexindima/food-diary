import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { TdeeInsight } from '../../models/tdee-insight.data';

@Component({
    selector: 'fd-tdee-insight-dialog-hint',
    imports: [DecimalPipe, FdUiIconComponent, TranslatePipe],
    templateUrl: './tdee-insight-dialog-hint.component.html',
    styleUrl: './tdee-insight-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogHintComponent {
    public readonly hintKey = input.required<string | null>();
    public readonly showSuggestion = input.required<boolean>();
    public readonly insight = input.required<TdeeInsight | null>();
}
