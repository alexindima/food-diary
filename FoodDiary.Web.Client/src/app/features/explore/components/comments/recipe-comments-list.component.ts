import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import type { RecipeComment } from '../../models/comment.data';
import { RecipeCommentItemComponent } from './recipe-comment-item.component';
import type { RecipeCommentViewModel } from './recipe-comments.types';

@Component({
    selector: 'fd-recipe-comments-list',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiLoaderComponent, RecipeCommentItemComponent],
    templateUrl: './recipe-comments-list.component.html',
    styleUrl: './recipe-comments.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCommentsListComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly commentsCount = input.required<number>();
    public readonly items = input.required<RecipeCommentViewModel[]>();
    public readonly hasMore = input.required<boolean>();

    public readonly edit = output<RecipeComment>();
    public readonly delete = output<RecipeComment>();
    public readonly loadMore = output();
}
