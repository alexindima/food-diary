import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiPaginationComponent, FdUiSelectComponent } from 'fd-ui-kit';
import type { Subscription } from 'rxjs';

import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue } from '../../../shared/period/admin-query';
import { OutgoingEmailDetailsComponent } from '../components/outgoing-email-details';
import { OutgoingStatusCountsComponent } from '../components/outgoing-status-counts';
import { AdminOutgoingEmailsFacade } from '../lib/admin-outgoing-emails.facade';
import type { OutgoingEmail } from '../models/outgoing-email';

@Component({
    selector: 'fd-admin-outgoing-emails',
    imports: [
        OutgoingStatusCountsComponent,
        CommonModule,
        AdminPeriodControlComponent,
        RouterLink,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiPaginationComponent,
        OutgoingEmailDetailsComponent,
    ],
    templateUrl: './admin-outgoing-emails.html',
    styleUrl: './admin-outgoing-emails.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminOutgoingEmailsComponent {
    private readonly api = inject(AdminOutgoingEmailsFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    private request?: Subscription;
    private readonly translate = inject(TranslateService);
    private readonly language = toSignal(this.translate.onLangChange);
    protected readonly purposeOptions = computed(() => {
        this.language();
        return [
            { value: '', label: String(this.translate.instant('ADMIN_OUTGOING.ALL')) },
            ...this.purposes.map(value => ({ value, label: String(this.translate.instant(`ADMIN_OUTGOING.PURPOSES.${value}`)) })),
        ];
    });
    protected readonly statusOptions = computed(() => {
        this.language();
        return [
            { value: '', label: String(this.translate.instant('ADMIN_OUTGOING.ALL')) },
            ...this.statuses.map(value => ({ value, label: String(this.translate.instant(`ADMIN_OUTGOING.STATUSES.${value}`)) })),
        ];
    });
    protected readonly messages = signal<OutgoingEmail[]>([]);
    protected readonly loading = signal(false);
    protected readonly failed = signal(false);
    protected readonly page = signal(1);
    protected readonly total = signal(0);
    protected readonly statusCounts = signal<Record<string, number> | null>(null);
    protected readonly purpose = signal('');
    protected readonly status = signal('');
    protected readonly recipient = signal('');
    protected readonly selected = signal<OutgoingEmail | null>(null);
    protected readonly purposes = [
        'other',
        'email_verification',
        'account_created',
        'password_reset',
        'bug_report_received',
        'dietologist_invitation',
        'template_test',
    ];
    protected readonly statuses = ['pending', 'processing', 'retry', 'sent', 'failed', 'suppressed'];

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.purpose.set(params.get('purpose') ?? '');
            this.status.set(params.get('status') ?? '');
            this.recipient.set(params.get('recipient') ?? '');
            this.page.set(adminPage(params.get('page')));
            this.fetch();
        });
    }

    protected load(reset = false): void {
        if (reset) {
            this.page.set(1);
        }
        const tree = this.router.createUrlTree([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                page: this.page(),
                purpose: adminQueryValue(this.purpose()),
                status: adminQueryValue(this.status()),
                recipient: adminQueryValue(this.recipient()),
                id: reset ? null : this.route.snapshot.queryParamMap.get('id'),
            },
        });
        if (this.router.serializeUrl(tree) === this.router.url) {
            this.fetch();
        } else {
            void this.router.navigateByUrl(tree);
        }
    }

    private fetch(): void {
        this.request?.unsubscribe();
        this.statusCounts.set(null);
        this.failed.set(false);
        this.selected.set(null);
        const range = adminPeriod(this.route.snapshot.queryParamMap);
        if (range === null) {
            this.messages.set([]);
            this.total.set(0);
            this.loading.set(false);
            return;
        }
        this.loading.set(true);
        this.request = this.api
            .getPage(this.page(), this.purpose(), this.status(), {
                recipient: this.recipient(),
                ...adminUtcPeriod(range),
                id: this.route.snapshot.queryParamMap.get('id') ?? '',
                correlationId: this.route.snapshot.queryParamMap.get('correlationId') ?? '',
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.messages.set(result.items);
                    if (this.route.snapshot.queryParamMap.has('id')) {
                        this.selected.set(result.items[0] ?? null);
                    }
                    this.total.set(result.totalItems);
                    this.statusCounts.set(result.statusCounts ?? null);
                    this.loading.set(false);
                },
                error: () => {
                    this.messages.set([]);
                    this.total.set(0);
                    this.failed.set(true);
                    this.loading.set(false);
                },
            });
    }

    protected goToPage(index: number): void {
        this.page.set(index + 1);
        this.load();
    }

    protected closeDetails(): void {
        this.selected.set(null);
        if (this.route.snapshot.queryParamMap.has('id')) {
            void this.router.navigate([], { relativeTo: this.route, queryParamsHandling: 'merge', queryParams: { id: null, page: 1 } });
        }
    }
}
