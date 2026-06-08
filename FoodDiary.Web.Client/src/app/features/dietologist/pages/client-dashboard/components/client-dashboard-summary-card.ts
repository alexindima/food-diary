import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { ClientBodyMeasurementView } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-summary-card',
    imports: [DatePipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './client-dashboard-summary-card.html',
    styleUrl: './client-dashboard-summary-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardSummaryCardComponent {
    public readonly titleKey = input('');
    public readonly summary = input<ClientBodyMeasurementView | null>(null);
}
