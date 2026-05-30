import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { ExploreInteractionsFacade } from '../../lib/explore-interactions.facade';

@Component({
    selector: 'fd-like-button',
    templateUrl: './like-button.html',
    styleUrls: ['./like-button.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiButtonComponent],
})
export class LikeButtonComponent {
    private readonly exploreInteractionsFacade = inject(ExploreInteractionsFacade);
    private readonly destroyRef = inject(DestroyRef);

    public readonly recipeId = input.required<string>();

    protected readonly isLiked = signal(false);
    protected readonly totalLikes = signal(0);
    protected readonly isToggling = signal(false);
    protected readonly icon = computed(() => (this.isLiked() ? 'favorite' : 'favorite_border'));

    public constructor() {
        effect(() => {
            this.exploreInteractionsFacade
                .getLikeStatus(this.recipeId())
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(status => {
                    this.isLiked.set(status.isLiked);
                    this.totalLikes.set(status.totalLikes);
                });
        });
    }

    protected onToggle(): void {
        if (this.isToggling()) {
            return;
        }

        this.isToggling.set(true);
        this.exploreInteractionsFacade
            .toggleLike(this.recipeId())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: status => {
                    this.isLiked.set(status.isLiked);
                    this.totalLikes.set(status.totalLikes);
                    this.isToggling.set(false);
                },
                error: () => {
                    this.isToggling.set(false);
                },
            });
    }
}
