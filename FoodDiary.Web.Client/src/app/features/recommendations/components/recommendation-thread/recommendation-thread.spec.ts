import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { RecommendationComment } from '../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
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
            createComment: vi.fn(() => of(createComment({ id: entityId<'recommendation-comment'>('new-comment'), text: 'Thanks' }))),
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

    it('hides the empty state on load failure and restores messages after retry', () => {
        facade.getComments.mockReturnValueOnce(throwError(() => new Error('failed')));
        const fixture = createComponent();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).not.toContain('RECOMMENDATIONS.DISCUSSION.EMPTY');
        expect(host.textContent).toContain('RECOMMENDATIONS.DISCUSSION.LOAD_ERROR');

        fixture.componentInstance['retryLoad']();
        fixture.detectChanges();
        expect(host.textContent).toContain('Please clarify');
        expect(fixture.componentInstance['loadFailed']()).toBe(false);
        expect(fixture.componentInstance['errorKey']()).toBeNull();
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
        id: entityId<'recommendation-comment'>('comment-1'),
        recommendationId: entityId<'recommendation'>('recommendation-1'),
        authorUserId: entityId<'user'>('user-1'),
        authorFirstName: 'Ada',
        authorLastName: 'Lovelace',
        authorEmail: 'ada@example.com',
        text: 'Please clarify',
        createdAtUtc: utcInstant('2026-07-24T07:00:00Z'),
        ...overrides,
    };
}
