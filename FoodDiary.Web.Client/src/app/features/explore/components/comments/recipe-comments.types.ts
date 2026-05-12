import type { RecipeComment } from '../../models/comment.data';

export interface RecipeCommentViewModel {
    comment: RecipeComment;
    authorLabel: string;
    dateLabel: string;
}
