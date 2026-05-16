import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { RecipeComment } from '../models/comment.data';
import { CommentService } from './comment.service';

const BASE_URL = environment.apiUrls.recipes;
const PAGE = 1;
const LIMIT = 10;

let service: CommentService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [CommentService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CommentService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('CommentService', () => {
    it('gets paged comments', () => {
        const page = createPage();

        service.getComments('recipe-1', PAGE, LIMIT).subscribe(result => {
            expect(result).toEqual(page);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/recipe-1/comments` && request.method === 'GET');
        expect(req.request.params.get('page')).toBe(String(PAGE));
        expect(req.request.params.get('limit')).toBe(String(LIMIT));
        req.flush(page);
    });

    it('returns empty page on comments load failure', () => {
        service.getComments('recipe-1', PAGE, LIMIT).subscribe(result => {
            expect(result.data).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/recipe-1/comments?page=${PAGE}&limit=${LIMIT}`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Server Error' });
    });

    it('creates, updates, and deletes comments', () => {
        const comment = createComment();

        service.createComment('recipe-1', { text: 'Created' }).subscribe(result => {
            expect(result).toEqual(comment);
        });
        const createReq = httpMock.expectOne(`${BASE_URL}/recipe-1/comments`);
        expect(createReq.request.method).toBe('POST');
        expect(createReq.request.body).toEqual({ text: 'Created' });
        createReq.flush(comment);

        service.updateComment('recipe-1', 'comment-1', { text: 'Updated' }).subscribe(result => {
            expect(result).toEqual(comment);
        });
        const updateReq = httpMock.expectOne(`${BASE_URL}/recipe-1/comments/comment-1`);
        expect(updateReq.request.method).toBe('PATCH');
        expect(updateReq.request.body).toEqual({ text: 'Updated' });
        updateReq.flush(comment);

        service.deleteComment('recipe-1', 'comment-1').subscribe(result => {
            expect(result).toBeNull();
        });
        const deleteReq = httpMock.expectOne(`${BASE_URL}/recipe-1/comments/comment-1`);
        expect(deleteReq.request.method).toBe('DELETE');
        deleteReq.flush(null);
    });
});

function createPage(): PageOf<RecipeComment> {
    return {
        data: [createComment()],
        page: PAGE,
        limit: LIMIT,
        totalPages: 1,
        totalItems: 1,
    };
}

function createComment(): RecipeComment {
    return {
        id: 'comment-1',
        recipeId: 'recipe-1',
        authorId: 'user-1',
        authorUsername: 'alexi',
        authorFirstName: null,
        text: 'Nice',
        createdAtUtc: '2026-05-16T10:00:00.000Z',
        modifiedAtUtc: null,
        isOwnedByCurrentUser: true,
    };
}
