import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { filter, finalize, switchMap } from 'rxjs';

import { CommentService } from '../../api/comment.service';
import type { RecipeComment } from '../../models/comment.data';
import { COMMENT_MAX_LENGTH, COMMENTS_PAGE_SIZE } from './recipe-comments-lib/recipe-comments.constants';
import { buildRecipeCommentViewModels } from './recipe-comments-lib/recipe-comments.mapper';
import type { RecipeCommentViewModel } from './recipe-comments-lib/recipe-comments.types';
import { RecipeCommentsListComponent } from './recipe-comments-list/recipe-comments-list.component';

@Component({
    selector: 'fd-recipe-comments',
    templateUrl: './recipe-comments.component.html',
    styleUrls: ['./recipe-comments.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ReactiveFormsModule, TranslatePipe, FdUiButtonComponent, FdUiTextareaComponent, RecipeCommentsListComponent],
})
export class RecipeCommentsComponent {
    private readonly commentService = inject(CommentService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public readonly recipeId = input.required<string>();

    public readonly comments = signal<RecipeComment[]>([]);
    public readonly isLoading = signal(false);
    public readonly totalItems = signal(0);
    public readonly currentPage = signal(1);
    public readonly pageSize = COMMENTS_PAGE_SIZE;
    public readonly commentControl = new FormControl('', [Validators.required, Validators.maxLength(COMMENT_MAX_LENGTH)]);
    public readonly editingCommentId = signal<string | null>(null);
    public readonly isSubmitting = signal(false);
    public readonly hasMore = computed(() => this.comments().length < this.totalItems());
    public readonly submitLabelKey = computed(() => (this.editingCommentId() !== null ? 'COMMON.SAVE' : 'COMMENTS.POST'));
    public readonly commentItems = computed<RecipeCommentViewModel[]>(() => {
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
            this.commentControl.reset();
            this.loadComments(1);
        });
    }

    public onSubmit(): void {
        const text = (this.commentControl.value ?? '').trim();
        if (text.length === 0 || this.commentControl.invalid || this.isSubmitting()) {
            return;
        }

        const editId = this.editingCommentId();
        this.isSubmitting.set(true);

        const operation =
            editId !== null
                ? this.commentService.updateComment(this.recipeId(), editId, { text })
                : this.commentService.createComment(this.recipeId(), { text });

        operation
            .pipe(
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.commentControl.reset();
                this.editingCommentId.set(null);
                this.currentPage.set(1);
                this.loadComments(1);
            });
    }

    public onEdit(comment: RecipeComment): void {
        this.editingCommentId.set(comment.id);
        this.commentControl.setValue(comment.text);
    }

    public onCancelEdit(): void {
        this.editingCommentId.set(null);
        this.commentControl.reset();
    }

    public onDelete(comment: RecipeComment): void {
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
                switchMap(() => this.commentService.deleteComment(this.recipeId(), comment.id)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.loadComments(this.currentPage());
            });
    }

    public onLoadMore(): void {
        const nextPage = this.currentPage() + 1;
        this.currentPage.set(nextPage);
        this.loadComments(nextPage, true);
    }

    private loadComments(page: number, append = false): void {
        this.isLoading.set(true);
        this.commentService
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
