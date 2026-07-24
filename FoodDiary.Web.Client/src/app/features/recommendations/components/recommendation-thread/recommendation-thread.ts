import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import type { RecommendationComment } from '../../../../shared/models/dietologist.data';
import { RecommendationsFacade } from '../../lib/recommendations.facade';

const COMMENT_MAX_LENGTH = 2000;

@Component({
    selector: 'fd-recommendation-thread',
    imports: [LocalizedDatePipe, TranslatePipe, FdUiButtonComponent],
    templateUrl: './recommendation-thread.html',
    styleUrl: './recommendation-thread.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecommendationThreadComponent {
    private readonly recommendationsFacade = inject(RecommendationsFacade);
    private readonly destroyRef = inject(DestroyRef);

    public readonly recommendationId = input.required<string>();

    protected readonly comments = signal<RecommendationComment[]>([]);
    protected readonly draft = signal('');
    protected readonly loading = signal(true);
    protected readonly saving = signal(false);
    protected readonly errorKey = signal<string | null>(null);

    public constructor() {
        effect(() => {
            const recommendationId = this.recommendationId();
            this.loadComments(recommendationId);
        });
    }

    protected updateDraft(event: Event): void {
        const target = event.target;
        const value = target instanceof HTMLTextAreaElement ? target.value : '';
        this.draft.set(value.slice(0, COMMENT_MAX_LENGTH));
    }

    protected submit(): void {
        const text = this.draft().trim();
        if (text.length === 0 || this.saving()) {
            return;
        }

        this.saving.set(true);
        this.errorKey.set(null);
        this.recommendationsFacade
            .createComment(this.recommendationId(), { text })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: comment => {
                    this.comments.update(comments => [...comments, comment]);
                    this.draft.set('');
                    this.saving.set(false);
                },
                error: () => {
                    this.errorKey.set('RECOMMENDATIONS.DISCUSSION.SAVE_ERROR');
                    this.saving.set(false);
                },
            });
    }

    protected authorName(comment: RecommendationComment): string {
        const name = `${comment.authorFirstName ?? ''} ${comment.authorLastName ?? ''}`.trim();
        return name.length > 0 ? name : comment.authorEmail;
    }

    private loadComments(recommendationId: string): void {
        this.loading.set(true);
        this.errorKey.set(null);
        this.recommendationsFacade
            .getComments(recommendationId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: comments => {
                    this.comments.set(comments);
                    this.loading.set(false);
                },
                error: () => {
                    this.comments.set([]);
                    this.errorKey.set('RECOMMENDATIONS.DISCUSSION.LOAD_ERROR');
                    this.loading.set(false);
                },
            });
    }
}
