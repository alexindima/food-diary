import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import { catchError, combineLatest, map, of, startWith, Subject, switchMap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage } from '../../../shared/period/admin-query';
import { AdminBugReportCardComponent } from '../components/admin-bug-report-card';
import { AdminBugsFacade } from '../lib/admin-bugs.facade';
import type { AdminBugReportPage } from '../models/admin-bug-report';

const SEARCH_MAX_LENGTH = 320;
const STATUS_MAX_LENGTH = 32;

@Component({
    selector: 'fd-admin-bugs',
    imports: [
        AdminBugReportCardComponent,
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
    templateUrl: './admin-bugs.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBugsPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly api = inject(AdminBugsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly refresh = new Subject<void>();
    protected readonly result = signal<AdminBugReportPage | null>(null);
    protected readonly loading = signal(true);
    protected readonly failed = signal(false);
    protected readonly page = signal(1);
    protected readonly detailId = signal<string | null>(null);
    protected readonly formModel = signal({ search: '', status: '' });
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.applyAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            maxLength(path.search, SEARCH_MAX_LENGTH);
            maxLength(path.status, STATUS_MAX_LENGTH);
        },
        { submission: { action: this.submitFormAsync } },
    );

    public constructor() {
        combineLatest([this.route.paramMap, this.route.queryParamMap, this.refresh.pipe(startWith(undefined))])
            .pipe(
                switchMap(([path, query]) => {
                    this.result.set(null);
                    this.failed.set(false);
                    this.detailId.set(path.get('id'));
                    this.page.set(adminPage(query.get('page')));
                    this.formModel.set({ search: query.get('search') ?? '', status: query.get('status') ?? '' });
                    const period = adminPeriod(query);
                    if (period === null) {
                        this.loading.set(false);
                        return of(null);
                    }
                    this.loading.set(true);
                    const params: Record<string, string | number> = { page: this.page(), limit: 25, ...this.formModel() };
                    const range = adminUtcPeriod(period);
                    Object.assign(params, range);
                    const id = this.detailId();
                    if (id !== null) {
                        params['id'] = id;
                        params['page'] = 1;
                    }
                    return this.api.getPage(params).pipe(
                        map(result => result),
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
        return this.router.navigate(['/bugs'], { queryParams: { ...this.formModel(), page: 1 }, queryParamsHandling: 'merge' });
    }

    protected goToPage(page: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page }, queryParamsHandling: 'merge' });
    }

    protected retry(): void {
        this.refresh.next();
    }

    protected safePullRequestUrl(value: string | null): string | null {
        if (value === null || value.length === 0) {
            return null;
        }
        try {
            const url = new URL(value);
            return url.protocol === 'https:' && url.username.length === 0 && url.password.length === 0 ? url.href : null;
        } catch {
            return null;
        }
    }
}
