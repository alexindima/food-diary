import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { UserFacade } from '../../../shared/lib/user.facade';
import { RecipeImportFacade } from './recipe-import.facade';

const ai = { importRecipe: vi.fn(), importRecipeVideo: vi.fn() };
const auth = { isPremium: vi.fn() };
const user = { user: signal<{ aiConsentAcceptedAt: string | null }>({ aiConsentAcceptedAt: 'accepted' }), acceptAiConsent: vi.fn() };
const dialogs = { open: vi.fn() };
let facade: RecipeImportFacade;
const OVERSIZED_VIDEO_BYTES = 53_477_376;

beforeEach(() => {
    ai.importRecipe.mockReset().mockReturnValue(of({ name: 'Salad' }));
    ai.importRecipeVideo.mockReset().mockReturnValue(of({ name: 'Salad' }));
    auth.isPremium.mockReset().mockReturnValue(true);
    user.user.set({ aiConsentAcceptedAt: 'accepted' });
    user.acceptAiConsent.mockReset().mockReturnValue(of(undefined));
    dialogs.open.mockReset().mockReturnValue({ afterClosed: () => of(false) });
    TestBed.configureTestingModule({
        providers: [
            RecipeImportFacade,
            { provide: AiFoodService, useValue: ai },
            { provide: AuthService, useValue: auth },
            { provide: UserFacade, useValue: user },
            { provide: FdUiDialogService, useValue: dialogs },
        ],
    });
    facade = TestBed.inject(RecipeImportFacade);
});

describe('recipe import', () => {
    it('submits an uploaded video through the video endpoint after access checks', async () => {
        const file = new File(['video'], 'recipe.mp4', { type: 'video/mp4' });
        await facade.recognizeAsync('', ' caption ', true, file);
        expect(ai.importRecipeVideo).toHaveBeenCalledWith({ sourceUrl: null, text: 'caption' }, file);
        expect(ai.importRecipe).not.toHaveBeenCalled();
        expect(facade.busy()).toBe(false);
    });
    it('can recognize video speech from a URL without an uploaded file', async () => {
        await facade.recognizeAsync('https://example.org/reel', '', true);
        expect(ai.importRecipeVideo).toHaveBeenCalledWith({ sourceUrl: 'https://example.org/reel', text: null }, null);
    });
    it('rejects video mode with caption only or an oversized file before calling the API', async () => {
        await facade.recognizeAsync('', 'caption', true);
        expect(facade.errorKey()).toContain('VIDEO_REQUIRED');
        const file = new File(['video'], 'recipe.mp4');
        Object.defineProperty(file, 'size', { value: OVERSIZED_VIDEO_BYTES });
        await facade.recognizeAsync('', '', true, file);
        expect(facade.errorKey()).toContain('VIDEO_INVALID');
        expect(ai.importRecipeVideo).not.toHaveBeenCalled();
    });
    it('does not send video when AI consent is declined', async () => {
        user.user.set({ aiConsentAcceptedAt: null });
        await facade.recognizeAsync('https://example.org/reel', '', true);
        expect(ai.importRecipeVideo).not.toHaveBeenCalled();
    });
    it('submits trimmed caption and URL only on recognize', async () => {
        expect(ai.importRecipe).not.toHaveBeenCalled();
        await facade.recognizeAsync(' https://example.org/recipe ', ' caption ');
        expect(ai.importRecipe).toHaveBeenCalledWith({ sourceUrl: 'https://example.org/recipe', text: 'caption' });
        expect(facade.busy()).toBe(false);
    });
    it('does not submit without AI access', async () => {
        auth.isPremium.mockReturnValue(false);
        expect(await facade.recognizeAsync('https://example.org', '')).toBeNull();
        expect(ai.importRecipe).not.toHaveBeenCalled();
        expect(facade.errorKey()).toContain('PREMIUM_REQUIRED');
    });
    it('does not submit when consent is declined', async () => {
        user.user.set({ aiConsentAcceptedAt: null });
        expect(await facade.recognizeAsync('', 'caption')).toBeNull();
        expect(ai.importRecipe).not.toHaveBeenCalled();
        expect(user.acceptAiConsent).not.toHaveBeenCalled();
    });
    it('clears loading after a source failure and allows pasted caption retry', async () => {
        ai.importRecipe.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 502 })));
        expect(await facade.recognizeAsync('https://example.org', '')).toBeNull();
        expect(facade.busy()).toBe(false);
        expect(facade.errorKey()).toContain('SOURCE_ERROR');
        ai.importRecipe.mockReturnValue(of({ name: 'Salad' }));
        expect(await facade.recognizeAsync('https://example.org', 'caption')).toEqual({ name: 'Salad' });
        expect(facade.errorKey()).toBeNull();
    });
});
