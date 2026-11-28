import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength } from '@angular/forms/signals';
import { ActivatedRoute, convertToParamMap, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminRouteInputsMatch, restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, firstAdminQueryValue } from '../../../shared/period/admin-query';
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
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly actorUserId = input(undefined, { transform: firstAdminQueryValue });
    public readonly subjectClientUserId = input(undefined, { transform: firstAdminQueryValue });
    public readonly action = input(undefined, { transform: firstAdminQueryValue });
    public readonly targetType = input(undefined, { transform: firstAdminQueryValue });
    public readonly targetId = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly api = inject(AdminAuditFacade);
    private readonly queryInputs = computed(() => ({
        page: this.page(),
        actorUserId: this.actorUserId(),
        subjectClientUserId: this.subjectClientUserId(),
        action: this.action(),
        targetType: this.targetType(),
        targetId: this.targetId(),
        period: this.period(),
        from: this.from(),
        to: this.to(),
    }));
    private readonly appliedQuery = computed(() => {
        const { page, actorUserId, subjectClientUserId, action, targetType, targetId, period, from, to } = this.queryInputs();
        return {
            page: adminPage(page ?? null),
            filters: {
                actorUserId: actorUserId ?? '',
                subjectClientUserId: subjectClientUserId ?? '',
                action: action ?? '',
                targetType: targetType ?? '',
                targetId: targetId ?? '',
            },
            periodParams: convertToParamMap({ period: period ?? null, from: from ?? null, to: to ?? null }),
        };
    });
    private readonly requestedPage = linkedSignal(() => this.buildRequest());
    private readonly pageResource = rxResource({
        params: this.requestedPage,
        stream: ({ params }) =>
            this.api.getPage(params).pipe(
                map(result => {
                    if (this.requestedPage() !== params || !adminRouteInputsMatch(this.route, this.queryInputs())) {
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
    protected readonly currentPage = computed(() => this.appliedQuery().page);
    protected readonly formModel = linkedSignal(() => this.appliedQuery().filters);
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
        this.formModel.set(this.appliedQuery().filters);
        this.requestedPage.set(this.buildRequest());
    }

    private buildRequest(): AuditPageQuery | undefined {
        const { page, filters, periodParams } = this.appliedQuery();
        const period = adminPeriod(periodParams);
        return period === null ? undefined : { page, limit: PAGE_SIZE, ...filters, ...adminUtcPeriod(period) };
    }
}
