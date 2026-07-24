import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { RecommendationComment } from '../../../../shared/models/dietologist.data';
import { RecommendationsFacade } from '../../lib/recommendations.facade';
import { RecommendationThreadComponent } from './recommendation-thread';

describe('RecommendationThreadComponent', () => {
    let facade: {
        getComments: ReturnType<typeof vi.fn>;
        createComment: ReturnType<typeof vi.fn>;
    };

    beforeEach(() => {
        facade = {
            getComments: vi.fn(() => of([createComment()])),
            createComment: vi.fn(() => of(createComment({ id: 'new-comment', text: 'Thanks' }))),
        };
    });

    it('loads the discussion in chronological API order', () => {
        const fixture = createComponent();

        expect(facade.getComments).toHaveBeenCalledWith('recommendation-1');
        expect(fixture.componentInstance['comments']()).toEqual([createComment()]);
        expect(fixture.componentInstance['loading']()).toBe(false);
    });

    it('posts a trimmed message and appends the confirmed response', () => {
        const fixture = createComponent();
        fixture.componentInstance['draft'].set('  Thanks  ');

        fixture.componentInstance['submit']();

        expect(facade.createComment).toHaveBeenCalledWith('recommendation-1', { text: 'Thanks' });
        expect(fixture.componentInstance['comments']().at(-1)?.id).toBe('new-comment');
        expect(fixture.componentInstance['draft']()).toBe('');
    });

    it('keeps the draft and exposes an error when posting fails', () => {
        facade.createComment.mockReturnValueOnce(throwError(() => new Error('failed')));
        const fixture = createComponent();
        fixture.componentInstance['draft'].set('Question');

        fixture.componentInstance['submit']();

        expect(fixture.componentInstance['draft']()).toBe('Question');
        expect(fixture.componentInstance['errorKey']()).toBe('RECOMMENDATIONS.DISCUSSION.SAVE_ERROR');
    });

    function createComponent(): ComponentFixture<RecommendationThreadComponent> {
        TestBed.configureTestingModule({
            imports: [RecommendationThreadComponent],
            providers: [provideTranslateTesting(), { provide: RecommendationsFacade, useValue: facade }],
        });
        const fixture = TestBed.createComponent(RecommendationThreadComponent);
        fixture.componentRef.setInput('recommendationId', 'recommendation-1');
        fixture.detectChanges();
        return fixture;
    }
});

function createComment(overrides: Partial<RecommendationComment> = {}): RecommendationComment {
    return {
        id: 'comment-1',
        recommendationId: 'recommendation-1',
        authorUserId: 'user-1',
        authorFirstName: 'Ada',
        authorLastName: 'Lovelace',
        authorEmail: 'ada@example.com',
        text: 'Please clarify',
        createdAtUtc: '2026-07-24T07:00:00Z',
        ...overrides,
    };
}
