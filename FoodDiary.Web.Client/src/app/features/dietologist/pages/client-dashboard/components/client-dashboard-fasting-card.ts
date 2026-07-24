import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { LocalizedDatePipe } from '../../../../../shared/i18n/localized-date.pipe';
import type { ClientFastingView } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-fasting-card',
    imports: [LocalizedDatePipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './client-dashboard-fasting-card.html',
    styleUrl: './client-dashboard-fasting-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardFastingCardComponent {
    public readonly fasting = input<ClientFastingView | null>(null);
    public readonly visible = input(false);
}
