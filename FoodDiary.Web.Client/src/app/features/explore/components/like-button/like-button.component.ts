import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { LikeService } from '../../api/like.service';

@Component({
    selector: 'fd-like-button',
    templateUrl: './like-button.component.html',
    styleUrls: ['./like-button.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconModule],
})
export class LikeButtonComponent implements OnInit {
    private readonly likeService = inject(LikeService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly recipeId = input.required<string>();

    public readonly isLiked = signal(false);
    public readonly totalLikes = signal(0);
    public readonly isToggling = signal(false);

    public ngOnInit(): void {
        this.likeService
            .getStatus(this.recipeId())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(status => {
                this.isLiked.set(status.isLiked);
                this.totalLikes.set(status.totalLikes);
            });
    }

    public onToggle(): void {
        if (this.isToggling()) {
            return;
        }

        this.isToggling.set(true);
        this.likeService
            .toggle(this.recipeId())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: status => {
                    this.isLiked.set(status.isLiked);
                    this.totalLikes.set(status.totalLikes);
                    this.isToggling.set(false);
                },
                error: () => this.isToggling.set(false),
            });
    }
}
