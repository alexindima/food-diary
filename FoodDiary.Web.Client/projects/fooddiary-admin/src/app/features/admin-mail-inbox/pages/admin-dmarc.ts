import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminDmarcReportsComponent } from '../components/admin-dmarc-reports';
import { type AdminDmarcPeriod, loadAdminDmarcView } from '../lib/admin-dmarc-loader';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';

@Component({
    selector: 'fd-admin-dmarc',
    imports: [
        DatePipe,
        DecimalPipe,
        PercentPipe,
        TranslatePipe,
        FdUiCardComponent,
        FdUiSegmentedToggleComponent,
        AdminLoadErrorComponent,
        AdminDmarcReportsComponent,
    ],
    templateUrl: './admin-dmarc.html',
    styleUrl: './admin-dmarc.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDmarcComponent {
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly facade = inject(AdminMailInboxFacade);

    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    protected readonly selectedPeriod = computed<AdminDmarcPeriod>(() => (this.period() === '30' ? '30' : '7'));
    protected readonly periods: Array<FdUiSegmentedToggleOption<AdminDmarcPeriod>> = [
        { value: '7', labelKey: 'ADMIN_DMARC.PERIOD_7' },
        { value: '30', labelKey: 'ADMIN_DMARC.PERIOD_30' },
    ];
    protected readonly reportResource = rxResource({
        params: this.selectedPeriod,
        stream: ({ params }) => loadAdminDmarcView(this.facade, params),
    });

    protected selectPeriod(period: AdminDmarcPeriod): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { period } });
    }

    protected retry(): void {
        this.reportResource.reload();
    }
}
