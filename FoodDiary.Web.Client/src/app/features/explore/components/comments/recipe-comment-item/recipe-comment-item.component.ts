import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { RecipeComment } from '../../../models/comment.data';
import type { RecipeCommentViewModel } from '../recipe-comments-lib/recipe-comments.types';

@Component({
    selector: 'fd-recipe-comment-item',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './recipe-comment-item.component.html',
    styleUrl: '../recipe-comments.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCommentItemComponent {
    public readonly item = input.required<RecipeCommentViewModel>();

    public readonly edit = output<RecipeComment>();
    public readonly delete = output<RecipeComment>();
}
