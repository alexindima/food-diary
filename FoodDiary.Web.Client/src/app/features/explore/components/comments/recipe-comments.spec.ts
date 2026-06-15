import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { ExploreInteractionsFacade } from '../../lib/explore-interactions.facade';
import type { RecipeComment } from '../../models/comment.data';
import { RecipeCommentsComponent } from './recipe-comments';
import { COMMENTS_PAGE_SIZE } from './recipe-comments-lib/recipe-comments.constants';

const TOTAL_ITEMS = 2;

let fixture: ComponentFixture<RecipeCommentsComponent>;
let component: RecipeCommentsComponent;
let commentService: ExploreInteractionsFacadeMock;
let dialogService: { open: ReturnType<typeof vi.fn> };

beforeEach(() => {
    commentService = createExploreInteractionsFacadeMock();
    dialogService = {
        open: vi.fn(() => ({
            afterClosed: (): Observable<boolean> => of(true),
        })),
    };

    TestBed.configureTestingModule({
        imports: [RecipeCommentsComponent],
        providers: [
            provideTranslateTesting(),
            { provide: ExploreInteractionsFacade, useValue: commentService },
            { provide: FdUiDialogService, useValue: dialogService },
        ],
    });

    fixture = TestBed.createComponent(RecipeCommentsComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('recipeId', 'recipe-1');
    fixture.detectChanges();
});

describe('RecipeCommentsComponent', () => {
    it('loads comments for the active recipe', () => {
        expect(commentService.getComments).toHaveBeenCalledWith('recipe-1', 1, COMMENTS_PAGE_SIZE);
        expect(component['comments']()).toEqual([createComment()]);
        expect(component['totalItems']()).toBe(TOTAL_ITEMS);
    });

    it('creates trimmed comments and reloads the first page', () => {
        component['commentForm'].text().value.set('  Created comment  ');

        component['onSubmit']();

        expect(commentService.createComment).toHaveBeenCalledWith('recipe-1', { text: 'Created comment' });
        expect(component['editingCommentId']()).toBeNull();
        expect(component['commentModel']().text).toBe('');
    });

    it('does not submit blank comments', () => {
        component['commentForm'].text().value.set('   ');

        component['onSubmit']();

        expect(commentService.createComment).not.toHaveBeenCalled();
    });

    it('updates an edited comment', () => {
        const comment = createComment({ text: 'Old text' });

        component['onEdit'](comment);
        component['commentForm'].text().value.set('Updated text');
        component['onSubmit']();

        expect(component['editingCommentId']()).toBeNull();
        expect(commentService.updateComment).toHaveBeenCalledWith('recipe-1', 'comment-1', { text: 'Updated text' });
    });

    it('deletes comments after confirmation', () => {
        component['onDelete'](createComment());

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(commentService.deleteComment).toHaveBeenCalledWith('recipe-1', 'comment-1');
    });

    it('appends next page on load more', () => {
        commentService.getComments.mockReturnValueOnce(of(createPage([createComment({ id: 'comment-2', text: 'Second' })])));

        component['onLoadMore']();

        expect(component['currentPage']()).toBe(2);
        expect(component['comments']().map(comment => comment.id)).toEqual(['comment-1', 'comment-2']);
    });
});

type ExploreInteractionsFacadeMock = {
    getComments: ReturnType<typeof vi.fn>;
    createComment: ReturnType<typeof vi.fn>;
    updateComment: ReturnType<typeof vi.fn>;
    deleteComment: ReturnType<typeof vi.fn>;
};

function createExploreInteractionsFacadeMock(): ExploreInteractionsFacadeMock {
    return {
        getComments: vi.fn(() => of(createPage([createComment()]))),
        createComment: vi.fn(() => of(createComment())),
        updateComment: vi.fn(() => of(createComment())),
        deleteComment: vi.fn(() => of(void 0)),
    };
}

function createPage(data: RecipeComment[]): PageOf<RecipeComment> {
    return {
        data,
        page: 1,
        limit: COMMENTS_PAGE_SIZE,
        totalPages: 1,
        totalItems: TOTAL_ITEMS,
    };
}

function createComment(overrides: Partial<RecipeComment> = {}): RecipeComment {
    return {
        id: 'comment-1',
        recipeId: 'recipe-1',
        authorId: 'user-1',
        authorUsername: 'alexi',
        authorFirstName: 'Alex',
        text: 'Nice recipe',
        createdAtUtc: '2026-05-16T10:00:00.000Z',
        modifiedAtUtc: null,
        isOwnedByCurrentUser: true,
        ...overrides,
    };
}
