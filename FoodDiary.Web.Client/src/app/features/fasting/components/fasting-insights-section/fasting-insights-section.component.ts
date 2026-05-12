import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert.component';

import type { FastingMessageViewModel } from '../../pages/fasting-page.types';

@Component({
    selector: 'fd-fasting-insights-section',
    imports: [TranslatePipe, FdUiInlineAlertComponent],
    templateUrl: './fasting-insights-section.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingInsightsSectionComponent {
    public readonly insights = input.required<readonly FastingMessageViewModel[]>();
}
