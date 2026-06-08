import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { ClientMetricTile } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-metric-list',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './client-dashboard-metric-list.html',
    styleUrl: './client-dashboard-metric-list.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardMetricListComponent {
    public readonly titleKey = input('');
    public readonly tiles = input<readonly ClientMetricTile[]>([]);
    public readonly wide = input(false);
}
