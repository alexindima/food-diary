import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { resolveRussianPluralCategory } from '../../../../shared/i18n/russian-plural.utils';
import type { FastingSession } from '../../models/fasting.data';
import type { FastingHistorySessionViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingHistoryCheckInListComponent } from '../fasting-history-check-in-list/fasting-history-check-in-list';

@Component({
    selector: 'fd-fasting-history-item',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent, FdUiButtonComponent, FastingHistoryCheckInListComponent],
    templateUrl: './fasting-history-item.html',
    styleUrls: ['./fasting-history-item.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingHistoryItemComponent {
    public readonly historyItem = input.required<FastingHistorySessionViewModel>();
    protected readonly checkInCountKey = computed(
        () => `FASTING.HISTORY_CHECK_INS_COUNT_${resolveRussianPluralCategory(this.historyItem().checkInCount).toUpperCase()}`,
    );

    public readonly chartOpen = output<FastingSession>();
    public readonly historyToggle = output<string>();
    public readonly checkInsLoadMore = output<string>();
}
