import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    type ElementRef,
    inject,
    Injector,
    signal,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiSelectComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import type { Subscription } from 'rxjs';

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
    private queueRequest?: Subscription;
    private detailsRequest?: Subscription;
    protected readonly statusLabels = RECIPE_MODERATION_STATUS_LABELS;
    protected readonly pageSize = QUEUE_PAGE_SIZE;
    protected readonly page = signal(1);
    protected readonly status = signal<RecipeModerationStatus | undefined>(undefined);
    protected readonly queue = signal<AdminRecipeModerationPage | null>(null);
    protected readonly queueLoading = signal(false);
    protected readonly queueFailed = signal(false);
    protected readonly selectedId = signal<RecipeModerationRevisionId | null>(null);
    protected readonly selected = signal<AdminRecipeModerationItem | null>(null);
    protected readonly detailsLoading = signal(false);
    protected readonly detailsFailed = signal(false);
    protected readonly actionInProgress = signal(false);
    protected readonly actionError = signal<string | null>(null);
    protected readonly reason = signal('');

    public constructor() {
        this.loadQueue();
    }

    protected loadQueue(): void {
        this.queueRequest?.unsubscribe();
        this.queueLoading.set(true);
        this.queueFailed.set(false);
        this.queueRequest = this.moderation
            .list(this.page(), this.pageSize, this.status())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: queue => {
                    const page = adminPageWithinTotal(this.page(), queue.totalItems, this.pageSize);
                    if (page !== this.page()) {
                        this.goToPage(page);
                        return;
                    }
                    this.queue.set(queue);
                    this.queueLoading.set(false);
                },
                error: () => {
                    this.queueFailed.set(true);
                    this.queueLoading.set(false);
                },
            });
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
        this.reason.set('');
        this.actionError.set(null);
        this.selectedId.set(id);
        this.loadDetails();
        afterNextRender(() => this.detailPanel()?.nativeElement.focus(), { injector: this.injector });
    }

    protected loadDetails(): void {
        const id = this.selectedId();
        if (id === null) {
            return;
        }
        this.detailsRequest?.unsubscribe();
        this.selected.set(null);
        this.detailsLoading.set(true);
        this.detailsFailed.set(false);
        this.detailsRequest = this.moderation
            .get(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: item => {
                    this.selected.set(item);
                    this.detailsLoading.set(false);
                },
                error: () => {
                    this.detailsFailed.set(true);
                    this.detailsLoading.set(false);
                },
            });
    }

    protected submit(action: AdminRecipeModerationAction): void {
        const item = this.selected();
        if (item === null || this.actionInProgress()) {
            return;
        }

        const operation = this.moderation.applyAction(item.revisionId, action, this.reason());
        this.actionError.set(null);
        this.actionInProgress.set(true);
        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.actionInProgress.set(false);
                this.reason.set('');
                this.loadQueue();
                this.loadDetails();
            },
            error: (error: unknown) => {
                this.actionInProgress.set(false);
                if (error instanceof HttpErrorResponse && error.status === REVISION_CONFLICT_STATUS) {
                    this.actionError.set('ADMIN_RECIPE_MODERATION.STALE');
                    this.loadQueue();
                    this.loadDetails();
                    return;
                }
                this.actionError.set('ADMIN_MODERATION.ACTION_ERROR');
            },
        });
    }
}
