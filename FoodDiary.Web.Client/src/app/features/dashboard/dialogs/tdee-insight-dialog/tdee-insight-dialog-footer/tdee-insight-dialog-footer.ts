import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { TdeeInsightDialogAction } from '../tdee-insight-dialog-lib/tdee-insight-dialog.types';

@Component({
    selector: 'fd-tdee-insight-dialog-footer',
    imports: [FdUiButtonComponent, TranslatePipe],
    templateUrl: './tdee-insight-dialog-footer.html',
    styleUrl: '../tdee-insight-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogFooterComponent {
    public readonly showSuggestion = input.required<boolean>();
    public readonly isApplying = input(false);

    public readonly dialogClose = output<TdeeInsightDialogAction>();
    public readonly suggestionApply = output();
}
