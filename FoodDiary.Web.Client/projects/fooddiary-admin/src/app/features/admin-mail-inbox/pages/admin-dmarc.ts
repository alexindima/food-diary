import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit';
import { catchError, map, of, startWith, switchMap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminDmarcReportsComponent } from '../components/admin-dmarc-reports';
import { type AdminDmarcPeriod, type AdminDmarcView, loadAdminDmarcView } from '../lib/admin-dmarc-loader';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';

type DmarcViewState = { status: 'loading' | 'error' } | { status: 'ready'; data: AdminDmarcView };

const LOADING: DmarcViewState = { status: 'loading' };

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
    private readonly refresh = signal(0);

    public readonly period = input<string>();
    protected readonly selectedPeriod = computed<AdminDmarcPeriod>(() => (this.period() === '30' ? '30' : '7'));
    protected readonly periods: Array<FdUiSegmentedToggleOption<AdminDmarcPeriod>> = [
        { value: '7', labelKey: 'ADMIN_DMARC.PERIOD_7' },
        { value: '30', labelKey: 'ADMIN_DMARC.PERIOD_30' },
    ];
    private readonly request = computed(() => ({ period: this.selectedPeriod(), refresh: this.refresh() }));

    protected readonly view = toSignal(
        toObservable(this.request).pipe(
            switchMap(({ period }) =>
                loadAdminDmarcView(this.facade, period).pipe(
                    map((data): DmarcViewState => ({ status: 'ready', data })),
                    catchError(() => of<DmarcViewState>({ status: 'error' })),
                    startWith(LOADING),
                ),
            ),
        ),
        { initialValue: LOADING },
    );

    protected selectPeriod(period: AdminDmarcPeriod): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { period } });
    }

    protected retry(): void {
        this.refresh.update(value => value + 1);
    }
}
