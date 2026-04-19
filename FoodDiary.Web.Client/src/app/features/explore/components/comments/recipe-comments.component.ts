import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { DatePipe } from '@angular/common';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { CommentService } from '../../api/comment.service';
import { RecipeComment } from '../../models/comment.data';

@Component({
    selector: 'fd-recipe-comments',
    templateUrl: './recipe-comments.component.html',
    styleUrls: ['./recipe-comments.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        DatePipe,
        FdUiButtonComponent,
        FdUiTextareaComponent,
        FdUiIconModule,
        FdUiLoaderComponent,
    ],
})
export class RecipeCommentsComponent implements OnInit {
    private readonly commentService = inject(CommentService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);

    public readonly recipeId = input.required<string>();

    public readonly comments = signal<RecipeComment[]>([]);
    public readonly isLoading = signal(false);
    public readonly totalItems = signal(0);
    public readonly currentPage = signal(1);
    public readonly pageSize = 10;
    public readonly commentControl = new FormControl('', [Validators.required, Validators.maxLength(2000)]);
    public readonly editingCommentId = signal<string | null>(null);
    public readonly isSubmitting = signal(false);
    public readonly hasMore = computed(() => this.comments().length < this.totalItems());

    public ngOnInit(): void {
        this.loadComments();
    }

    public onSubmit(): void {
        if (this.commentControl.invalid || this.isSubmitting()) {
            return;
        }

        const text = this.commentControl.value!.trim();
        const editId = this.editingCommentId();
        this.isSubmitting.set(true);

        const operation = editId
            ? this.commentService.updateComment(this.recipeId(), editId, { text })
            : this.commentService.createComment(this.recipeId(), { text });

        operation
            .pipe(
                finalize(() => this.isSubmitting.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.commentControl.reset();
                this.editingCommentId.set(null);
                this.currentPage.set(1);
                this.loadComments();
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
            .subscribe(confirmed => {
                if (confirmed) {
                    this.commentService
                        .deleteComment(this.recipeId(), comment.id)
                        .pipe(takeUntilDestroyed(this.destroyRef))
                        .subscribe(() => this.loadComments());
                }
            });
    }

    public onLoadMore(): void {
        this.currentPage.update(p => p + 1);
        this.loadComments(true);
    }

    private loadComments(append = false): void {
        this.isLoading.set(true);
        this.commentService
            .getComments(this.recipeId(), this.currentPage(), this.pageSize)
            .pipe(
                finalize(() => this.isLoading.set(false)),
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
