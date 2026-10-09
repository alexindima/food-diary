import { describe, expect, it } from 'vitest';

import { notificationFromSdk } from './notification-sdk.mapper';

describe('notificationFromSdk', () => {
    it('decodes a known kind and retains wire identity, timestamp precision and navigation', () => {
        const item = notificationFromSdk({
            id: 'legacy-id',
            type: 'NewRecommendationCommentForDietologist',
            title: 'Title',
            isRead: false,
            createdAtUtc: '2026-04-12T22:30:00.1234567Z',
            targetUrl: '/recommendations?recommendationId=rec-1',
            referenceId: 'unrelated-server-reference',
        });
        expect(item).toEqual({
            id: 'legacy-id',
            type: { kind: 'known', code: 'NewRecommendationCommentForDietologist' },
            title: 'Title',
            body: null,
            isRead: false,
            createdAtUtc: '2026-04-12T22:30:00.1234567Z',
            targetUrl: '/recommendations?recommendationId=rec-1',
            referenceId: 'unrelated-server-reference',
        });
    });

    it.each(['FutureNotification', 'constructor', 'toString', 'newrecommendation'])('keeps unknown code %s', code => {
        const item = notificationFromSdk({ id: 'n1', type: code, title: 'Title', isRead: true, createdAtUtc: 'invalid-legacy-time' });
        expect(item.type).toEqual({ kind: 'unknown', code });
        expect(item.createdAtUtc).toBe('invalid-legacy-time');
        expect(item.targetUrl).toBeNull();
    });
});
