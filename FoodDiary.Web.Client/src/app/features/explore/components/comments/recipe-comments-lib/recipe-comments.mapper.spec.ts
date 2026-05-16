import { describe, expect, it } from 'vitest';

import type { RecipeComment } from '../../../models/comment.data';
import { DEFAULT_COMMENT_AUTHOR_LABEL } from './recipe-comments.constants';
import { buildRecipeCommentViewModels, formatRecipeCommentDate } from './recipe-comments.mapper';

const VALID_DATE = '2026-05-16T10:30:00.000Z';

describe('buildRecipeCommentViewModels', () => {
    it('uses first name, username, or fallback author labels', () => {
        const result = buildRecipeCommentViewModels(
            [
                createComment({ id: 'comment-1', authorFirstName: 'Alex', authorUsername: 'alexi' }),
                createComment({ id: 'comment-2', authorFirstName: null, authorUsername: 'foodie' }),
                createComment({ id: 'comment-3', authorFirstName: null, authorUsername: null }),
            ],
            'en',
        );

        expect(result.map(item => item.authorLabel)).toEqual(['Alex', 'foodie', DEFAULT_COMMENT_AUTHOR_LABEL]);
    });
});

describe('formatRecipeCommentDate', () => {
    it('returns raw value for invalid dates', () => {
        expect(formatRecipeCommentDate('not-a-date', 'en')).toBe('not-a-date');
    });

    it('formats valid dates for the active language', () => {
        expect(formatRecipeCommentDate(VALID_DATE, 'en')).not.toBe(VALID_DATE);
    });
});

function createComment(overrides: Partial<RecipeComment> = {}): RecipeComment {
    return {
        id: 'comment-1',
        recipeId: 'recipe-1',
        authorId: 'user-1',
        authorUsername: 'alexi',
        authorFirstName: 'Alex',
        text: 'Nice recipe',
        createdAtUtc: VALID_DATE,
        modifiedAtUtc: null,
        isOwnedByCurrentUser: false,
        ...overrides,
    };
}
