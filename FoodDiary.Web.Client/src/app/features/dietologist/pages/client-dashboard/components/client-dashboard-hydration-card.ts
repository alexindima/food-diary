import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { ClientHydrationView } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-hydration-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './client-dashboard-hydration-card.html',
    styleUrl: './client-dashboard-hydration-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardHydrationCardComponent {
    public readonly hydration = input<ClientHydrationView | null>(null);
}
