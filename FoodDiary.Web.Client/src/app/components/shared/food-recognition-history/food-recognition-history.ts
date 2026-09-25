import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiStatusBadgeComponent, type FdUiStatusBadgeTone } from 'fd-ui-kit/status-badge/fd-ui-status-badge';
import { catchError, finalize, of, timer } from 'rxjs';

import { injectCurrentLanguage } from '../../../shared/i18n/inject-current-language';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';

const HISTORY_POLL_MS = 5000;

@Component({
    selector: 'fd-food-recognition-history',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiStatusBadgeComponent],
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
            return 'AI_RECOGNITION.EMPTY';
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
        if (!this.isCompleted(job) || this.loading() || this.disabled() || this.deleting().includes(job.id)) {
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
                },
                error: () => {
                    this.deleteFailed.set(true);
                },
            });
    }

    protected load(): void {
        if (this.loading() || this.disabled() || this.deleting().length > 0) {
            return;
        }
        this.loading.set(true);
        this.failed.set(false);
        this.facade
            .listRecognitions()
            .pipe(
                catchError(() => {
                    this.failed.set(true);
                    return of([]);
                }),
                finalize(() => {
                    this.loading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(jobs => {
                this.jobs.set(jobs.filter(job => (job.isProductLabel ?? false) === this.productLabel()));
                this.loaded.set(true);
            });
    }
}
