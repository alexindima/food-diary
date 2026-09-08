import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';
import { catchError, combineLatest, of, startWith, Subject, switchMap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { AdminRetentionFacade } from '../lib/admin-retention.facade';
import type { AdminRetentionReport } from '../models/admin-retention';

const PERCENT_SCALE = 100;

@Component({
    selector: 'fd-admin-retention',
    imports: [DatePipe, TranslatePipe, FdUiCardComponent, AdminPeriodControlComponent, AdminLoadErrorComponent],
    templateUrl: './admin-retention.html',
    styleUrl: './admin-retention.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { class: 'fd-grid fd-gap-md' },
})
export class AdminRetentionComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly api = inject(AdminRetentionFacade);
    private readonly translate = inject(TranslateService);
    private readonly refresh = new Subject<void>();
    protected readonly report = signal<AdminRetentionReport | null>(null);
    protected readonly loading = signal(true);
    protected readonly failed = signal(false);

    public constructor() {
        combineLatest([this.route.queryParamMap, this.refresh.pipe(startWith(undefined))])
            .pipe(
                switchMap(([query]) => {
                    this.report.set(null);
                    this.failed.set(false);
                    const range = adminPeriod(query, '30d');
                    if (range === null) {
                        this.loading.set(false);
                        return of(null);
                    }
                    this.loading.set(true);
                    return this.api.getReport({ ...range, from: range.from ?? '1970-01-01' }).pipe(
                        catchError(() => {
                            this.failed.set(true);
                            return of(null);
                        }),
                    );
                }),
                takeUntilDestroyed(),
            )
            .subscribe(report => {
                this.report.set(report);
                this.loading.set(false);
            });
    }

    protected retry(): void {
        this.refresh.next();
    }

    protected ratio(value: number | null, total: number): string {
        if (value === null) {
            return String(this.translate.instant('ADMIN_RETENTION.IMMATURE'));
        }
        return total === 0 ? '—' : `${((value / total) * PERCENT_SCALE).toFixed(1)}% (${value}/${total})`;
    }
}
