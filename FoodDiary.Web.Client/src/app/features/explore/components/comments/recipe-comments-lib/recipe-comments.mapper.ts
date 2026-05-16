import { resolveAppLocale } from '../../../../../shared/lib/locale.constants';
import type { RecipeComment } from '../../../models/comment.data';
import { DEFAULT_COMMENT_AUTHOR_LABEL } from './recipe-comments.constants';
import type { RecipeCommentViewModel } from './recipe-comments.types';

export function buildRecipeCommentViewModels(comments: RecipeComment[], language: string): RecipeCommentViewModel[] {
    return comments.map(comment => ({
        comment,
        authorLabel: comment.authorFirstName ?? comment.authorUsername ?? DEFAULT_COMMENT_AUTHOR_LABEL,
        dateLabel: formatRecipeCommentDate(comment.createdAtUtc, language),
    }));
}

export function formatRecipeCommentDate(value: string, language: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return new Intl.DateTimeFormat(resolveAppLocale(language), {
        dateStyle: 'short',
        timeStyle: 'short',
    }).format(date);
}
