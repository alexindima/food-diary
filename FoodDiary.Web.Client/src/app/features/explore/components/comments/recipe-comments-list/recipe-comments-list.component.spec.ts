import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { RecipeComment } from '../../../models/comment.data';
import type { RecipeCommentViewModel } from '../recipe-comments-lib/recipe-comments.types';
import { RecipeCommentsListComponent } from './recipe-comments-list.component';

describe('RecipeCommentsListComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [RecipeCommentsListComponent, TranslateModule.forRoot()],
        });
    });

    it('renders loader for initial load', () => {
        const fixture = createComponent({ isLoading: true, commentsCount: 0, items: [], hasMore: false });

        expect(getElement(fixture).querySelector('fd-ui-loader')).not.toBeNull();
    });

    it('renders empty state without comments', () => {
        const fixture = createComponent({ isLoading: false, commentsCount: 0, items: [], hasMore: false });
        const text = getElement(fixture).textContent;

        expect(text).toContain('COMMENTS.NO_COMMENTS');
        expect(text).toContain('COMMENTS.NO_COMMENTS_MESSAGE');
    });

    it('renders comments and emits load more', () => {
        const fixture = createComponent({ isLoading: false, commentsCount: 1, items: [createViewModel()], hasMore: true });
        const loadMore = vi.fn();
        fixture.componentInstance.loadMore.subscribe(loadMore);
        const element = getElement(fixture);

        expect(element.textContent).toContain('Nice recipe');
        getButtonByText(element, 'COMMENTS.LOAD_MORE').click();

        expect(loadMore).toHaveBeenCalledTimes(1);
    });
});

type ListInput = {
    isLoading: boolean;
    commentsCount: number;
    items: RecipeCommentViewModel[];
    hasMore: boolean;
};

function createComponent(input: ListInput): ComponentFixture<RecipeCommentsListComponent> {
    const fixture = TestBed.createComponent(RecipeCommentsListComponent);
    fixture.componentRef.setInput('isLoading', input.isLoading);
    fixture.componentRef.setInput('commentsCount', input.commentsCount);
    fixture.componentRef.setInput('items', input.items);
    fixture.componentRef.setInput('hasMore', input.hasMore);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<RecipeCommentsListComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getButtonByText(element: HTMLElement, text: string): HTMLElement {
    const button = Array.from(element.querySelectorAll<HTMLElement>('fd-ui-button')).find(item => item.textContent.includes(text));
    if (button === undefined) {
        throw new Error(`Button with text "${text}" was not found.`);
    }

    return button;
}

function createViewModel(): RecipeCommentViewModel {
    const comment: RecipeComment = {
        id: 'comment-1',
        recipeId: 'recipe-1',
        authorId: 'user-1',
        authorUsername: 'alexi',
        authorFirstName: 'Alex',
        text: 'Nice recipe',
        createdAtUtc: '2026-05-16T10:00:00.000Z',
        modifiedAtUtc: null,
        isOwnedByCurrentUser: false,
    };

    return {
        comment,
        authorLabel: 'Alex',
        dateLabel: '5/16/26, 10:00 AM',
    };
}
