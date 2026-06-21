import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { FastingHistorySessionViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingHistoryCheckInEntryComponent } from '../fasting-history-check-in-entry/fasting-history-check-in-entry';

@Component({
    selector: 'fd-fasting-history-check-in-list',
    imports: [TranslatePipe, FdUiButtonComponent, FastingHistoryCheckInEntryComponent],
    templateUrl: './fasting-history-check-in-list.html',
    styleUrls: ['./fasting-history-check-in-list.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingHistoryCheckInListComponent {
    public readonly historyItem = input.required<FastingHistorySessionViewModel>();
    public readonly checkInsLoadMore = output<string>();
}
