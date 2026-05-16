import type { RecipeComment } from '../../../models/comment.data';

export type RecipeCommentViewModel = {
    comment: RecipeComment;
    authorLabel: string;
    dateLabel: string;
};
