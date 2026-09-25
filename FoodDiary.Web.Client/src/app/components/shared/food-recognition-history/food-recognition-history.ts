import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiEmptyStateComponent, FdUiHintDirective, FdUiPaginationComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiStatusBadgeComponent, type FdUiStatusBadgeTone } from 'fd-ui-kit/status-badge/fd-ui-status-badge';
import { catchError, finalize, of, switchMap, timer } from 'rxjs';

import { injectCurrentLanguage } from '../../../shared/i18n/inject-current-language';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { type FoodRecognitionJob, RECOGNITION_PAGE_SIZE } from '../../../shared/models/food-recognition.data';

const HISTORY_POLL_MS = 5000;

@Component({
    selector: 'fd-food-recognition-history',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FdUiEmptyStateComponent,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiHintDirective,
        FdUiStatusBadgeComponent,
        FdUiPaginationComponent,
    ],
    templateUrl: './food-recognition-history.html',
    styleUrl: './food-recognition-history.scss',
})
export class FoodRecognitionHistoryComponent {
    private readonly facade = inject(AiFoodFacade);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly jobs = signal<FoodRecognitionJob[]>([]);
    protected readonly loaded = signal(false);
    protected readonly loading = signal(false);
    protected readonly failed = signal(false);
    protected readonly language = injectCurrentLanguage();
    protected readonly deleting = signal<string[]>([]);
    protected readonly deleteFailed = signal(false);
    protected readonly deleted = signal(false);
    protected readonly pageSize = RECOGNITION_PAGE_SIZE;
    protected readonly pageIndex = signal(0);
    protected readonly totalItems = signal(0);
    protected readonly paginationDisabled = computed(() => this.loading() || this.disabled() || this.deleting().length > 0);
    public readonly countChanged = output<number>();
    public readonly backToRecognition = output();
    protected readonly showEmptyState = computed(
        () => this.screen() && this.loaded() && !this.loading() && !this.failed() && this.jobs().length === 0,
    );
    public readonly screen = input(false);
    public readonly compact = input(false);
    public readonly productLabel = input(false);
    public readonly disabled = input(false);
    public readonly recognitionSelected = output<FoodRecognitionJob>();

    public constructor() {
        effect(() => {
            if (this.screen()) {
                untracked(() => {
                    this.load();
                });
            }
        });
        timer(HISTORY_POLL_MS, HISTORY_POLL_MS)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                if (this.jobs().some(job => !this.isCompleted(job))) {
                    this.load();
                }
            });
    }

    protected isCompleted(job: FoodRecognitionJob): boolean {
        return job.status === 'Succeeded' || job.status === 'Failed';
    }

    protected title(job: FoodRecognitionJob): string | null {
        const label = job.vision?.productLabel?.name;
        const items = job.vision?.items.map(item => item.nameLocal ?? item.nameEn).join(', ') ?? '';
        return label ?? (items.length > 0 ? items : null);
    }

    protected fallbackTitle(job: FoodRecognitionJob): string {
        if (job.status === 'Succeeded') {
            return 'AI_RECOGNITION.NAME_UNKNOWN';
        }
        if (job.status === 'Failed') {
            return 'AI_RECOGNITION.STATUS.Failed';
        }
        return 'AI_RECOGNITION.UNKNOWN_PRODUCT';
    }

    protected statusTone(job: FoodRecognitionJob): FdUiStatusBadgeTone {
        if (job.status === 'Succeeded') {
            return 'success';
        }
        if (job.status === 'Failed') {
            return 'danger';
        }
        return 'muted';
    }

    protected readonly statusMessage = computed(() => {
        if (this.failed()) {
            return 'AI_RECOGNITION.LOAD_ERROR';
        }
        if (this.loading() && !this.loaded()) {
            return 'AI_RECOGNITION.LOADING';
        }
        if (this.loaded() && this.jobs().length === 0) {
            return this.screen() ? null : 'AI_RECOGNITION.EMPTY';
        }
        return null;
    });

    protected photoCount(job: FoodRecognitionJob): number {
        return 1 + (job.additionalImages?.length ?? 0);
    }

    protected date(value: string): string {
        return new Intl.DateTimeFormat(this.language(), {
            day: 'numeric',
            month: 'short',
            hour: '2-digit',
            minute: '2-digit',
            hour12: false,
        }).format(new Date(value));
    }

    protected remove(job: FoodRecognitionJob): void {
        if (!this.isCompleted(job) || this.loading() || this.disabled() || this.deleting().length > 0) {
            return;
        }
        this.deleting.update(ids => [...ids, job.id]);
        this.deleteFailed.set(false);
        this.deleted.set(false);
        this.facade
            .deleteRecognition(job.id)
            .pipe(
                finalize(() => {
                    this.deleting.update(ids => ids.filter(id => id !== job.id));
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.jobs.update(jobs => jobs.filter(item => item.id !== job.id));
                    this.deleted.set(true);
                    this.deleting.set([]);
                    const total = Math.max(0, this.totalItems() - 1);
                    this.totalItems.set(total);
                    this.countChanged.emit(total);
                    this.load(Math.min(this.pageIndex(), Math.max(0, Math.ceil(total / this.pageSize) - 1)));
                },
                error: () => {
                    this.deleteFailed.set(true);
                },
            });
    }

    protected load(pageIndex = this.pageIndex()): void {
        if (this.loading() || this.disabled() || this.deleting().length > 0) {
            return;
        }
        const previousPage = this.pageIndex();
        this.pageIndex.set(pageIndex);
        this.loading.set(true);
        this.failed.set(false);
        this.facade
            .listRecognitions(pageIndex + 1, this.pageSize, this.productLabel())
            .pipe(
                switchMap(page => {
                    const lastPage = Math.max(1, page.totalPages);
                    return page.page > lastPage ? this.facade.listRecognitions(lastPage, this.pageSize, this.productLabel()) : of(page);
                }),
                catchError(() => {
                    this.pageIndex.set(previousPage);
                    this.failed.set(true);
                    return of(null);
                }),
                finalize(() => {
                    this.loading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(page => {
                if (page === null) {
                    return;
                }
                this.jobs.set(page.data);
                this.pageIndex.set(page.page - 1);
                this.totalItems.set(page.totalItems);
                this.countChanged.emit(page.totalItems);
                this.loaded.set(true);
            });
    }
}
