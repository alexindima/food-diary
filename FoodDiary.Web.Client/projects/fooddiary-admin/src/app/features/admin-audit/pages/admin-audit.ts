import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength } from '@angular/forms/signals';
import { ActivatedRoute, type ParamMap, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage } from '../../../shared/period/admin-query';
import { AdminAuditFacade } from '../lib/admin-audit.facade';

const ACTION_MAX_LENGTH = 200;
const TARGETTYPE_MAX_LENGTH = 100;
const TARGETID_MAX_LENGTH = 200;
const PAGE_SIZE = 25;

type AuditPageQuery = Record<string, string | number> & { page: number };

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
    private readonly requestedPage = signal<AuditPageQuery | undefined>(undefined);
    private readonly pageResource = rxResource({
        params: this.requestedPage,
        stream: ({ params }) =>
            this.api.getPage(params).pipe(
                map(result => {
                    if (this.requestedPage() !== params) {
                        return null;
                    }
                    const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                        totalItems: result.totalItems,
                        pageSize: PAGE_SIZE,
                    });
                    return correctingPage ? null : result;
                }),
            ),
    });
    protected readonly result = computed(() => (this.pageResource.hasValue() ? this.pageResource.value() : null));
    // Keep the loading state while navigation to the corrected page is pending.
    protected readonly loading = computed(
        () => this.pageResource.isLoading() || (this.pageResource.hasValue() && this.pageResource.value() === null),
    );
    protected readonly failed = computed(() => this.pageResource.error() !== undefined);
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
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(query => {
            this.loadPage(query);
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
        this.loadPage(this.route.snapshot.queryParamMap);
    }

    private loadPage(query: ParamMap): void {
        const page = adminPage(query.get('page'));
        const filters = {
            actorUserId: query.get('actorUserId') ?? '',
            subjectClientUserId: query.get('subjectClientUserId') ?? '',
            action: query.get('action') ?? '',
            targetType: query.get('targetType') ?? '',
            targetId: query.get('targetId') ?? '',
        };
        this.page.set(page);
        this.formModel.set(filters);
        const period = adminPeriod(query);
        this.requestedPage.set(period === null ? undefined : { page, limit: PAGE_SIZE, ...filters, ...adminUtcPeriod(period) });
    }
}
