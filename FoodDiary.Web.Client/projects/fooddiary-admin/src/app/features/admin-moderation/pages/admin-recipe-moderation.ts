import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    type ElementRef,
    inject,
    Injector,
    linkedSignal,
    signal,
    viewChild,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiSelectComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPageWithinTotal } from '../../../shared/period/admin-pagination';
import { AdminModerationNavigationComponent } from '../components/admin-moderation-navigation';
import { AdminRecipeModerationDetailComponent } from '../components/admin-recipe-moderation-detail';
import { type AdminRecipeModerationAction, AdminRecipeModerationFacade } from '../lib/admin-recipe-moderation.facade';
import {
    type AdminRecipeModerationItem,
    type AdminRecipeModerationPage,
    RECIPE_MODERATION_STATUS_LABELS,
    RECIPE_MODERATION_STATUSES,
    type RecipeModerationRevisionId,
    type RecipeModerationStatus,
} from '../models/admin-recipe-moderation.data';

const QUEUE_PAGE_SIZE = 20;
const REVISION_CONFLICT_STATUS = 409;

@Component({
    selector: 'fd-admin-recipe-moderation',
    imports: [
        AdminLoadErrorComponent,
        AdminModerationNavigationComponent,
        AdminRecipeModerationDetailComponent,
        DatePipe,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        FdUiSelectComponent,
        TranslatePipe,
    ],
    templateUrl: './admin-recipe-moderation.html',
    styleUrl: './admin-recipe-moderation.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminRecipeModerationComponent {
    private readonly moderation = inject(AdminRecipeModerationFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly detailPanel = viewChild<ElementRef<HTMLElement>>('detailPanel');
    private readonly queueRequest = signal<{ page: number; status: RecipeModerationStatus | undefined }>({ page: 1, status: undefined });
    private readonly queueResource = rxResource({
        params: this.queueRequest,
        stream: ({ params }) =>
            this.moderation.list(params.page, this.pageSize, params.status).pipe(
                map(queue => {
                    if (this.queueRequest() !== params) {
                        return null;
                    }
                    const page = adminPageWithinTotal(params.page, queue.totalItems, this.pageSize);
                    if (page !== params.page) {
                        this.goToPage(page);
                        return null;
                    }
                    return queue;
                }),
            ),
    });
    private readonly detailsRequest = signal<{ id: RecipeModerationRevisionId } | undefined>(undefined);
    private readonly detailsResource = rxResource({
        params: this.detailsRequest,
        stream: ({ params }) => this.moderation.get(params.id),
    });
    protected readonly statusLabels = RECIPE_MODERATION_STATUS_LABELS;
    protected readonly pageSize = QUEUE_PAGE_SIZE;
    protected readonly page = signal(1);
    protected readonly status = signal<RecipeModerationStatus | undefined>(undefined);
    protected readonly queue = linkedSignal({
        source: this.queueResource.snapshot,
        computation: (snapshot, previous): AdminRecipeModerationPage | null =>
            (snapshot.status === 'resolved' ? snapshot.value : null) ?? previous?.value ?? null,
    });
    protected readonly queueLoading = computed(
        () => this.queueResource.isLoading() || (this.queueResource.hasValue() && this.queueResource.value() === null),
    );
    protected readonly queueFailed = computed(() => this.queueResource.error() !== undefined);
    protected readonly selectedId = computed(() => this.detailsRequest()?.id ?? null);
    protected readonly selected = computed<AdminRecipeModerationItem | null>(() =>
        this.detailsResource.hasValue() ? this.detailsResource.value() : null,
    );
    protected readonly detailsLoading = this.detailsResource.isLoading;
    protected readonly detailsFailed = computed(() => this.detailsResource.error() !== undefined);
    protected readonly actionInProgress = signal(false);
    protected readonly actionError = signal<string | null>(null);
    protected readonly reason = signal('');

    protected loadQueue(): void {
        this.queueRequest.set({ page: this.page(), status: this.status() });
    }

    protected setStatus(value: unknown): void {
        this.status.set(RECIPE_MODERATION_STATUSES.find(status => status === value));
        this.goToPage(1);
    }

    protected goToPage(page: number): void {
        this.page.set(page);
        this.loadQueue();
    }

    protected select(id: RecipeModerationRevisionId): void {
        if (this.actionInProgress()) {
            return;
        }

        this.reason.set('');
        this.actionError.set(null);
        this.loadDetails(id);
        afterNextRender(() => this.detailPanel()?.nativeElement.focus(), { injector: this.injector });
    }

    protected retryDetails(id: RecipeModerationRevisionId): void {
        if (this.selectedId() === id) {
            this.loadDetails(id);
        }
    }

    private loadDetails(id: RecipeModerationRevisionId): void {
        this.detailsRequest.set({ id });
    }

    protected submit(action: AdminRecipeModerationAction): void {
        const item = this.selected();
        if (item === null || this.actionInProgress()) {
            return;
        }

        const revisionId = item.revisionId;
        const operation = this.moderation.applyAction(revisionId, action, this.reason());
        this.actionError.set(null);
        this.actionInProgress.set(true);
        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.actionInProgress.set(false);
                this.reason.set('');
                this.loadQueue();
                this.loadDetails(revisionId);
            },
            error: (error: unknown) => {
                this.actionInProgress.set(false);
                if (error instanceof HttpErrorResponse && error.status === REVISION_CONFLICT_STATUS) {
                    this.actionError.set('ADMIN_RECIPE_MODERATION.STALE');
                    this.loadQueue();
                    this.loadDetails(revisionId);
                    return;
                }
                this.actionError.set('ADMIN_MODERATION.ACTION_ERROR');
            },
        });
    }
}
