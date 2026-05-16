import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { FastingSession } from '../../models/fasting.data';
import type { FastingHistorySessionViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingHistoryItemComponent } from '../fasting-history-item/fasting-history-item.component';

@Component({
    selector: 'fd-fasting-history-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FastingHistoryItemComponent],
    templateUrl: './fasting-history-card.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingHistoryCardComponent {
    public readonly historyItems = input.required<readonly FastingHistorySessionViewModel[]>();
    public readonly canLoadMoreHistory = input.required<boolean>();
    public readonly isLoadingMoreHistory = input.required<boolean>();

    public readonly chartOpen = output<FastingSession>();
    public readonly historyToggle = output<string>();
    public readonly checkInsLoadMore = output<string>();
    public readonly historyLoadMore = output();
}
