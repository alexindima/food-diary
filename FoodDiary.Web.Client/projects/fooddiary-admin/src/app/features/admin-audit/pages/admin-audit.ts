import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import { catchError, combineLatest, of, startWith, Subject, switchMap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage } from '../../../shared/period/admin-query';
import { AdminAuditFacade } from '../lib/admin-audit.facade';
import type { AdminAuditPageResult } from '../models/admin-audit';

const ACTION_MAX_LENGTH = 200;
const TARGETTYPE_MAX_LENGTH = 100;
const TARGETID_MAX_LENGTH = 200;

@Component({
    selector: 'fd-admin-audit',
    imports: [
        DatePipe,
        TranslatePipe,
        RouterLink,
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiInputComponent,
        AdminLoadErrorComponent,
        AdminPeriodControlComponent,
    ],
    styleUrl: '../../../shared/admin-records.scss',
    templateUrl: './admin-audit.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAuditPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly api = inject(AdminAuditFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly refresh = new Subject<void>();
    protected readonly result = signal<AdminAuditPageResult | null>(null);
    protected readonly loading = signal(true);
    protected readonly failed = signal(false);
    protected readonly page = signal(1);
    protected readonly formModel = signal({
        actorUserId: '',
        subjectClientUserId: '',
        action: '',
        targetType: '',
        targetId: '',
    });
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.applyAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            maxLength(path.action, ACTION_MAX_LENGTH);
            maxLength(path.targetType, TARGETTYPE_MAX_LENGTH);
            maxLength(path.targetId, TARGETID_MAX_LENGTH);
        },
        { submission: { action: this.submitFormAsync } },
    );

    public constructor() {
        combineLatest([this.route.queryParamMap, this.refresh.pipe(startWith(undefined))])
            .pipe(
                switchMap(([query]) => {
                    this.result.set(null);
                    this.failed.set(false);
                    this.page.set(adminPage(query.get('page')));
                    this.formModel.set({
                        actorUserId: query.get('actorUserId') ?? '',
                        subjectClientUserId: query.get('subjectClientUserId') ?? '',
                        action: query.get('action') ?? '',
                        targetType: query.get('targetType') ?? '',
                        targetId: query.get('targetId') ?? '',
                    });
                    const period = adminPeriod(query);
                    if (period === null) {
                        this.loading.set(false);
                        return of(null);
                    }
                    this.loading.set(true);
                    return this.api.getPage({ page: this.page(), limit: 25, ...this.formModel(), ...adminUtcPeriod(period) }).pipe(
                        catchError(() => {
                            this.failed.set(true);
                            return of(null);
                        }),
                    );
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(result => {
                this.result.set(result);
                this.loading.set(false);
            });
    }

    protected async applyAsync(): Promise<boolean> {
        return this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { ...this.formModel(), page: 1 },
            queryParamsHandling: 'merge',
        });
    }

    protected goToPage(page: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page }, queryParamsHandling: 'merge' });
    }

    protected retry(): void {
        this.refresh.next();
    }
}
