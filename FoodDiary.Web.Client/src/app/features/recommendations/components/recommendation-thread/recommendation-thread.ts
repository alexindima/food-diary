import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import type { RecommendationComment } from '../../../../shared/models/dietologist.data';
import { entityId } from '../../../../shared/models/semantics/entity-id';
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
    protected readonly locale = injectCurrentLanguage();
    private readonly recommendationsFacade = inject(RecommendationsFacade);
    private readonly destroyRef = inject(DestroyRef);

    public readonly recommendationId = input.required<string>();

    private readonly commentsResource = rxResource({
        params: this.recommendationId,
        stream: ({ params }) => this.recommendationsFacade.getComments(entityId<'recommendation'>(params)),
    });
    protected readonly comments = linkedSignal(() => (this.commentsResource.hasValue() ? this.commentsResource.value() : []));
    protected readonly draft = signal('');
    protected readonly loading = this.commentsResource.isLoading;
    protected readonly loadFailed = computed(() => this.commentsResource.error() !== undefined);
    protected readonly saving = signal(false);
    private readonly saveError = linkedSignal<string, string | null>({ source: this.recommendationId, computation: () => null });
    protected readonly errorKey = computed(() => (this.loadFailed() ? 'RECOMMENDATIONS.DISCUSSION.LOAD_ERROR' : this.saveError()));

    protected updateDraft(value: string): void {
        this.draft.set(value.slice(0, COMMENT_MAX_LENGTH));
    }

    protected submit(): void {
        const text = this.draft().trim();
        if (text.length === 0 || this.loading() || this.loadFailed() || this.saving()) {
            return;
        }

        const recommendationId = this.recommendationId();
        this.saving.set(true);
        this.saveError.set(null);
        this.recommendationsFacade
            .createComment(entityId<'recommendation'>(recommendationId), { text })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: comment => {
                    if (this.recommendationId() === recommendationId) {
                        this.comments.update(comments => [...comments, comment]);
                        this.draft.set('');
                    }
                    this.saving.set(false);
                },
                error: () => {
                    if (this.recommendationId() === recommendationId) {
                        this.saveError.set('RECOMMENDATIONS.DISCUSSION.SAVE_ERROR');
                    }
                    this.saving.set(false);
                },
            });
    }

    protected authorName(comment: RecommendationComment): string | null {
        const name = `${comment.authorFirstName ?? ''} ${comment.authorLastName ?? ''}`.trim();
        return name.length > 0 ? name : comment.authorEmail;
    }

    protected retryLoad(): void {
        this.saveError.set(null);
        this.commentsResource.reload();
    }
}
