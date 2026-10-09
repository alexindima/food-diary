import { describe, expect, it } from 'vitest';

import type { ContentReportHttpResponse } from '../../../shared/api/sdk/generated/model/content-report-http-response';
import type { RecipeCommentHttpResponse } from '../../../shared/api/sdk/generated/model/recipe-comment-http-response';
import { commentFromSdk, contentReportFromSdk } from './community-sdk.mapper';

const PRECISE_INSTANT = '2026-05-16T10:00:00.1234567Z';
const COMMENT: RecipeCommentHttpResponse = {
    id: 'legacy/comment',
    recipeId: 'local:recipe',
    authorId: 'legacy/user',
    authorUsername: null,
    authorFirstName: null,
    text: 'Комментарий',
    createdAtUtc: PRECISE_INSTANT,
    isOwnedByCurrentUser: false,
};
const REPORT: ContentReportHttpResponse = {
    id: 'report',
    reporterId: 'user',
    targetType: 'Recipe',
    targetId: 'recipe',
    reason: 'Spam',
    status: 'Pending',
    createdAtUtc: PRECISE_INSTANT,
};

describe('community SDK decoding', () => {
    it('preserves opaque comment identities, text, author snapshots and timestamp precision', () => {
        expect(commentFromSdk(COMMENT)).toEqual(COMMENT);
    });

    it.each([undefined, null, PRECISE_INSTANT])('retains optional comment modification time %s', modifiedAtUtc => {
        const response = { ...COMMENT, modifiedAtUtc };
        expect(commentFromSdk(response)).toEqual(response);
    });

    it('keeps malformed historical instants verbatim for the existing display fallback', () => {
        expect(commentFromSdk({ ...COMMENT, createdAtUtc: 'not-a-date' }).createdAtUtc).toBe('not-a-date');
    });

    it.each([
        { targetType: 'Recipe', targetId: 'recipe', target: { kind: 'recipe', recipeId: 'recipe' } },
        { targetType: 'Comment', targetId: 'comment', target: { kind: 'comment', commentId: 'comment' } },
        { targetType: 'FutureTarget', targetId: 'opaque', target: { kind: 'unknown', targetType: 'FutureTarget', targetId: 'opaque' } },
    ])('decodes $targetType without changing the observed identity', ({ targetType, targetId, target }) => {
        const response = { ...REPORT, targetType, targetId };
        const { targetType: _type, targetId: _id, ...fields } = response;
        expect(contentReportFromSdk(response)).toEqual({ ...fields, target });
    });

    it.each([undefined, null, PRECISE_INSTANT])('retains optional report review time %s', reviewedAtUtc => {
        expect(contentReportFromSdk({ ...REPORT, reviewedAtUtc }).reviewedAtUtc).toBe(reviewedAtUtc);
    });

    it('still rejects missing required comment and report fields', () => {
        expect(() => commentFromSdk({ ...COMMENT, recipeId: undefined })).toThrow();
        expect(() => contentReportFromSdk({ ...REPORT, targetId: undefined })).toThrow();
    });
});
