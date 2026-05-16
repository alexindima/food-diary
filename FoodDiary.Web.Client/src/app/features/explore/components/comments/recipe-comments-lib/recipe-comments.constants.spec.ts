import { describe, expect, it } from 'vitest';

import { COMMENT_MAX_LENGTH, COMMENTS_PAGE_SIZE, DEFAULT_COMMENT_AUTHOR_LABEL } from './recipe-comments.constants';

const EXPECTED_COMMENTS_PAGE_SIZE = 10;
const EXPECTED_COMMENT_MAX_LENGTH = 2_000;

describe('recipe comments constants', () => {
    it('keeps pagination and validation defaults stable', () => {
        expect(COMMENTS_PAGE_SIZE).toBe(EXPECTED_COMMENTS_PAGE_SIZE);
        expect(COMMENT_MAX_LENGTH).toBe(EXPECTED_COMMENT_MAX_LENGTH);
        expect(DEFAULT_COMMENT_AUTHOR_LABEL).toBe('User');
    });
});
