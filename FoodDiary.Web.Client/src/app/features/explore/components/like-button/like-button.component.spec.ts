import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LikeService } from '../../api/like.service';
import type { RecipeLikeStatus } from '../../models/like.data';
import { LikeButtonComponent } from './like-button.component';

const INITIAL_LIKES = 2;
const UPDATED_LIKES = 3;

let fixture: ComponentFixture<LikeButtonComponent>;
let component: LikeButtonComponent;
let likeService: LikeServiceMock;

beforeEach(() => {
    likeService = createLikeServiceMock();

    TestBed.configureTestingModule({
        imports: [LikeButtonComponent, TranslateModule.forRoot()],
        providers: [{ provide: LikeService, useValue: likeService }],
    });

    fixture = TestBed.createComponent(LikeButtonComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('recipeId', 'recipe-1');
    fixture.detectChanges();
});

describe('LikeButtonComponent', () => {
    it('loads like status for the recipe', () => {
        expect(likeService.getStatus).toHaveBeenCalledWith('recipe-1');
        expect(component.isLiked()).toBe(false);
        expect(component.totalLikes()).toBe(INITIAL_LIKES);
        expect(component.icon()).toBe('favorite_border');
    });

    it('toggles like status', () => {
        component.onToggle();

        expect(likeService.toggle).toHaveBeenCalledWith('recipe-1');
        expect(component.isLiked()).toBe(true);
        expect(component.totalLikes()).toBe(UPDATED_LIKES);
        expect(component.isToggling()).toBe(false);
        expect(component.icon()).toBe('favorite');
    });

    it('resets toggling state on toggle failure', () => {
        likeService.toggle.mockReturnValueOnce(throwError(() => new Error('failed')));

        component.onToggle();

        expect(component.isToggling()).toBe(false);
    });
});

type LikeServiceMock = {
    getStatus: ReturnType<typeof vi.fn>;
    toggle: ReturnType<typeof vi.fn>;
};

function createLikeServiceMock(): LikeServiceMock {
    return {
        getStatus: vi.fn(() => of(createStatus({ isLiked: false, totalLikes: INITIAL_LIKES }))),
        toggle: vi.fn(() => of(createStatus({ isLiked: true, totalLikes: UPDATED_LIKES }))),
    };
}

function createStatus(overrides: Partial<RecipeLikeStatus>): RecipeLikeStatus {
    return {
        isLiked: false,
        totalLikes: 0,
        ...overrides,
    };
}
