import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';
import { LocalizedDatePipe } from '../../../../../shared/i18n/localized-date.pipe';
import type { ClientBodyMeasurementView } from '../client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard-summary-card',
    imports: [LocalizedDatePipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './client-dashboard-summary-card.html',
    styleUrl: './client-dashboard-summary-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientDashboardSummaryCardComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly titleKey = input('');
    public readonly summary = input<ClientBodyMeasurementView | null>(null);
}
