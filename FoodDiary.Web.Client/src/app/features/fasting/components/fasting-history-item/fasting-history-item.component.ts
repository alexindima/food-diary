import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { FastingSession } from '../../models/fasting.data';
import type { FastingHistorySessionViewModel } from '../../pages/fasting-page.types';
import { FastingHistoryCheckInListComponent } from '../fasting-history-check-in-list/fasting-history-check-in-list.component';

@Component({
    selector: 'fd-fasting-history-item',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent, FdUiButtonComponent, FastingHistoryCheckInListComponent],
    templateUrl: './fasting-history-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingHistoryItemComponent {
    public readonly historyItem = input.required<FastingHistorySessionViewModel>();

    public readonly chartOpen = output<FastingSession>();
    public readonly historyToggle = output<string>();
    public readonly checkInsLoadMore = output<string>();
}
