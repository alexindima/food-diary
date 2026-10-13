import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiPaginationComponent, FdUiSelectComponent, FdUiTabsComponent } from 'fd-ui-kit';
import { catchError, combineLatest, map, of, startWith, Subject, switchMap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage } from '../../../shared/period/admin-query';
import { AdminBugReportListComponent } from '../components/admin-bug-report-list';
import { REPORT_STATUSES, reportStatusPresentation } from '../lib/admin-bug-presentation';
import { AdminBugsFacade } from '../lib/admin-bugs.facade';
import type { AdminBugReportPage } from '../models/admin-bug-report';

const SEARCH_MAX_LENGTH = 320;
const STATUS_MAX_LENGTH = 32;
const PAGE_SIZE = 25;

@Component({
    selector: 'fd-admin-bugs',
    imports: [
        AdminBugReportListComponent,
        TranslatePipe,
        RouterLink,
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiPaginationComponent,
        AdminLoadErrorComponent,
        AdminPeriodControlComponent,
        FdUiTabsComponent,
    ],
    styleUrls: ['../../../shared/admin-records.scss', './admin-bugs.scss'],
    templateUrl: './admin-bugs.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBugsPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly api = inject(AdminBugsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly refresh = new Subject<void>();
    private readonly translate = inject(TranslateService);
    private readonly languageChange = toSignal(this.translate.onLangChange, { initialValue: null });
    protected readonly result = signal<AdminBugReportPage | null>(null);
    protected readonly loading = signal(true);
    protected readonly failed = signal(false);
    protected readonly page = signal(1);
    protected readonly detailId = signal<string | null>(null);
    protected readonly source = signal('email');
    protected readonly tabs = [
        { value: 'email', labelKey: 'ADMIN_BUGS.TAB_EMAIL' },
        { value: 'logs', labelKey: 'ADMIN_BUGS.TAB_LOGS' },
    ];
    protected readonly statusOptions = computed(() => {
        this.languageChange();
        return [
            { value: '', label: this.translatedLabel('ADMIN_COMMON.ALL') },
            ...REPORT_STATUSES.map(value => ({ value, label: this.translatedLabel(reportStatusPresentation(value).key) })),
        ];
    });
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
                    this.source.set(query.get('source') === 'logs' ? 'logs' : 'email');
                    this.formModel.set({
                        search: query.get('search') ?? '',
                        status: query.get('status') ?? '',
                    });
                    const id = this.detailId();
                    const period = id === null ? adminPeriod(query) : {};
                    if (period === null) {
                        this.loading.set(false);
                        return of(null);
                    }
                    this.loading.set(true);
                    const params: Record<string, string | number> =
                        id === null
                            ? { page: this.page(), limit: PAGE_SIZE, source: this.source(), ...this.formModel(), ...adminUtcPeriod(period) }
                            : { page: 1, limit: PAGE_SIZE, id };
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
                if (
                    result !== null &&
                    this.detailId() === null &&
                    restoreAdminPage(this.router, this.route, this.page(), { totalItems: result.totalItems, pageSize: PAGE_SIZE })
                ) {
                    return;
                }
                this.result.set(result);
                if (this.detailId() !== null && result?.items[0] !== undefined) {
                    this.source.set(result.items[0].source);
                }
                this.loading.set(false);
            });
    }

    private translatedLabel(key: string): string {
        const value: unknown = this.translate.instant(key);
        return typeof value === 'string' ? value : key;
    }

    protected async applyAsync(): Promise<boolean> {
        const navigated = await this.router.navigate(['/bugs'], {
            queryParams: { ...this.formModel(), source: this.source(), page: 1 },
            queryParamsHandling: 'merge',
        });
        if (!navigated) {
            this.refresh.next();
        }
        return navigated;
    }

    protected goToPage(page: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page }, queryParamsHandling: 'merge' });
    }

    protected selectSource(source: string): void {
        void this.router.navigate(['/bugs'], {
            queryParams: { source, page: 1, search: null, status: null },
            queryParamsHandling: 'merge',
        });
    }

    protected resetFilters(): void {
        void this.router.navigate(['/bugs'], {
            queryParams: { source: this.source(), page: 1, search: null, status: null, period: null, from: null, to: null },
            queryParamsHandling: 'merge',
        });
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
