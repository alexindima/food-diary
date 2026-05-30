import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';

import type { FastingMessageViewModel } from '../../pages/fasting-page-lib/fasting-page.types';

@Component({
    selector: 'fd-fasting-insights-section',
    imports: [TranslatePipe, FdUiInlineAlertComponent],
    templateUrl: './fasting-insights-section.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingInsightsSectionComponent {
    public readonly insights = input.required<readonly FastingMessageViewModel[]>();
}
