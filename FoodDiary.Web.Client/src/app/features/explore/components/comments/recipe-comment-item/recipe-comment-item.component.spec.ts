import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { RecipeComment } from '../../../models/comment.data';
import type { RecipeCommentViewModel } from '../recipe-comments-lib/recipe-comments.types';
import { RecipeCommentItemComponent } from './recipe-comment-item.component';

describe('RecipeCommentItemComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [RecipeCommentItemComponent, TranslateModule.forRoot()],
        });
    });

    it('renders comment metadata and edited state', () => {
        const fixture = createComponent(createViewModel());
        const text = getElement(fixture).textContent;

        expect(text).toContain('Alex');
        expect(text).toContain('Nice recipe');
        expect(text).toContain('COMMENTS.EDITED');
    });

    it('emits edit and delete for owned comments', () => {
        const fixture = createComponent(createViewModel());
        const edit = vi.fn();
        const deleteComment = vi.fn();
        fixture.componentInstance.edit.subscribe(edit);
        fixture.componentInstance.delete.subscribe(deleteComment);
        const element = getElement(fixture);

        getButtonByText(element, 'COMMENTS.EDIT').click();
        getButtonByText(element, 'COMMENTS.DELETE').click();

        expect(edit).toHaveBeenCalledWith(fixture.componentInstance.item().comment);
        expect(deleteComment).toHaveBeenCalledWith(fixture.componentInstance.item().comment);
    });

    it('hides actions for comments owned by another user', () => {
        const fixture = createComponent(createViewModel({ isOwnedByCurrentUser: false }));
        const element = getElement(fixture);

        expect(getOptionalButtonByText(element, 'COMMENTS.EDIT')).toBeUndefined();
        expect(getOptionalButtonByText(element, 'COMMENTS.DELETE')).toBeUndefined();
    });
});

function createComponent(item: RecipeCommentViewModel): ComponentFixture<RecipeCommentItemComponent> {
    const fixture = TestBed.createComponent(RecipeCommentItemComponent);
    fixture.componentRef.setInput('item', item);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<RecipeCommentItemComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getButtonByText(element: HTMLElement, text: string): HTMLElement {
    const button = getOptionalButtonByText(element, text);
    if (button === undefined) {
        throw new Error(`Button with text "${text}" was not found.`);
    }

    return button;
}

function getOptionalButtonByText(element: HTMLElement, text: string): HTMLElement | undefined {
    return Array.from(element.querySelectorAll<HTMLElement>('fd-ui-button')).find(item => item.textContent.includes(text));
}

function createViewModel(overrides: Partial<RecipeComment> = {}): RecipeCommentViewModel {
    const comment: RecipeComment = {
        id: 'comment-1',
        recipeId: 'recipe-1',
        authorId: 'user-1',
        authorUsername: 'alexi',
        authorFirstName: 'Alex',
        text: 'Nice recipe',
        createdAtUtc: '2026-05-16T10:00:00.000Z',
        modifiedAtUtc: '2026-05-16T10:10:00.000Z',
        isOwnedByCurrentUser: true,
        ...overrides,
    };

    return {
        comment,
        authorLabel: 'Alex',
        dateLabel: '5/16/26, 10:00 AM',
    };
}
