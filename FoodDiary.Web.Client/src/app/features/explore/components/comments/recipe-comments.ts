import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, maxLength, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { filter, finalize, switchMap } from 'rxjs';

import { ExploreInteractionsFacade } from '../../lib/explore-interactions.facade';
import type { RecipeComment } from '../../models/comment.data';
import { COMMENT_MAX_LENGTH, COMMENTS_PAGE_SIZE } from './recipe-comments-lib/recipe-comments.constants';
import { buildRecipeCommentViewModels } from './recipe-comments-lib/recipe-comments.mapper';
import type { RecipeCommentViewModel } from './recipe-comments-lib/recipe-comments.types';
import { RecipeCommentsListComponent } from './recipe-comments-list/recipe-comments-list';

@Component({
    selector: 'fd-recipe-comments',
    templateUrl: './recipe-comments.html',
    styleUrls: ['./recipe-comments.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormField, TranslatePipe, FdUiButtonComponent, FdUiTextareaComponent, RecipeCommentsListComponent],
})
export class RecipeCommentsComponent {
    private readonly exploreInteractionsFacade = inject(ExploreInteractionsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public readonly recipeId = input.required<string>();

    protected readonly comments = signal<RecipeComment[]>([]);
    protected readonly isLoading = signal(false);
    protected readonly totalItems = signal(0);
    protected readonly currentPage = signal(1);
    protected readonly pageSize = COMMENTS_PAGE_SIZE;
    protected readonly commentModel = signal({ text: '' });
    protected readonly commentForm = form(this.commentModel, path => {
        required(path.text);
        maxLength(path.text, COMMENT_MAX_LENGTH);
    });
    protected readonly editingCommentId = signal<string | null>(null);
    protected readonly isSubmitting = signal(false);
    protected readonly hasMore = computed(() => this.comments().length < this.totalItems());
    protected readonly submitLabelKey = computed(() => (this.editingCommentId() !== null ? 'COMMON.SAVE' : 'COMMENTS.POST'));
    protected readonly commentItems = computed<RecipeCommentViewModel[]>(() => {
        this.languageVersion();
        return buildRecipeCommentViewModels(this.comments(), this.translateService.getCurrentLang());
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        effect(() => {
            this.recipeId();
            this.currentPage.set(1);
            this.comments.set([]);
            this.totalItems.set(0);
            this.editingCommentId.set(null);
            this.commentModel.set({ text: '' });
            this.loadComments(1);
        });
    }

    protected onSubmit(): void {
        this.commentForm().markAsTouched();
        const text = this.commentModel().text.trim();
        if (text.length === 0 || this.commentForm().invalid() || this.isSubmitting()) {
            return;
        }

        const editId = this.editingCommentId();
        this.isSubmitting.set(true);

        const operation =
            editId !== null
                ? this.exploreInteractionsFacade.updateComment(this.recipeId(), editId, { text })
                : this.exploreInteractionsFacade.createComment(this.recipeId(), { text });

        operation
            .pipe(
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.commentModel.set({ text: '' });
                this.editingCommentId.set(null);
                this.currentPage.set(1);
                this.loadComments(1);
            });
    }

    protected onEdit(comment: RecipeComment): void {
        this.editingCommentId.set(comment.id);
        this.commentForm.text().value.set(comment.text);
    }

    protected onCancelEdit(): void {
        this.editingCommentId.set(null);
        this.commentModel.set({ text: '' });
    }

    protected onDelete(comment: RecipeComment): void {
        this.fdDialogService
            .open(FdUiConfirmDialogComponent, {
                size: 'sm',
                data: {
                    title: 'COMMENTS.DELETE_CONFIRM_TITLE',
                    message: 'COMMENTS.DELETE_CONFIRM_MESSAGE',
                },
            })
            .afterClosed()
            .pipe(
                filter((confirmed): confirmed is true => confirmed === true),
                switchMap(() => this.exploreInteractionsFacade.deleteComment(this.recipeId(), comment.id)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.loadComments(this.currentPage());
            });
    }

    protected onLoadMore(): void {
        const nextPage = this.currentPage() + 1;
        this.currentPage.set(nextPage);
        this.loadComments(nextPage, true);
    }

    private loadComments(page: number, append = false): void {
        this.isLoading.set(true);
        this.exploreInteractionsFacade
            .getComments(this.recipeId(), page, this.pageSize)
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(data => {
                this.totalItems.set(data.totalItems);
                if (append) {
                    this.comments.update(existing => [...existing, ...data.data]);
                } else {
                    this.comments.set(data.data);
                }
            });
    }
}
