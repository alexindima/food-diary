import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { ClientFastingView } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-fasting-card',
    imports: [DatePipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './client-dashboard-fasting-card.html',
    styleUrl: './client-dashboard-fasting-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardFastingCardComponent {
    public readonly fasting = input<ClientFastingView | null>(null);
}
