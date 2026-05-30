import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';

import type { FastingMessageViewModel } from '../../fasting-page-lib/fasting-page.types';

@Component({
    selector: 'fd-fasting-alerts-section',
    imports: [TranslatePipe, FdUiInlineAlertComponent],
    templateUrl: './fasting-alerts-section.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingAlertsSectionComponent {
    public readonly alerts = input.required<readonly FastingMessageViewModel[]>();

    public readonly promptDismiss = output<string>();
    public readonly promptSnooze = output<string>();
}
