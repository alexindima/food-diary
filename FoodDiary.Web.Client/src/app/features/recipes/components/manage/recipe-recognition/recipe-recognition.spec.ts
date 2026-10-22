import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../../services/auth.service';
import { AiFoodService } from '../../../../../shared/api/ai-food.service';
import { UserFacade } from '../../../../../shared/lib/user.facade';
import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import { BrowserClipboardService } from '../../../../../shared/platform/browser-clipboard.service';
import { BrowserStorageService } from '../../../../../shared/platform/browser-storage.service';
import { RecipeRecognitionComponent } from './recipe-recognition';

const reportedUrl = 'https://www.youtube.com/watch?v=ncD_3gaRKbM';
const caption = 'Ingredients: 100 g flour. Mix and cook.';
const draft: RecipeImportResult = {
    name: 'Pancakes',
    description: null,
    ingredients: [{ name: 'flour', amount: '100 g' }],
    steps: ['Mix and cook.'],
    servings: 1,
    prepMinutes: null,
    cookMinutes: null,
    authorNutrition: null,
    sourceUrl: reportedUrl,
};
const ai = { importRecipe: vi.fn<AiFoodService['importRecipe']>(), importRecipeVideo: vi.fn<AiFoodService['importRecipeVideo']>() };
let fixture: ComponentFixture<RecipeRecognitionComponent>;

beforeEach(async () => {
    ai.importRecipe.mockReset().mockReturnValue(of(draft));
    ai.importRecipeVideo.mockReset().mockReturnValue(of(draft));
    await TestBed.configureTestingModule({
        imports: [RecipeRecognitionComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AiFoodService, useValue: ai },
            { provide: AuthService, useValue: { isPremium: vi.fn().mockReturnValue(true) } },
            {
                provide: UserFacade,
                useValue: { user: signal({ aiConsentAcceptedAt: 'accepted' }), acceptAiConsent: vi.fn().mockReturnValue(of(undefined)) },
            },
            {
                provide: FdUiDialogService,
                useValue: { open: vi.fn().mockReturnValue({ afterClosed: vi.fn().mockReturnValue(of(false)) }) },
            },
            {
                provide: BrowserClipboardService,
                useValue: {
                    onReturn: vi.fn().mockReturnValue(vi.fn()),
                    readTextAsync: vi.fn().mockResolvedValue(''),
                    canReadWithoutPromptAsync: vi.fn().mockResolvedValue(false),
                },
            },
            { provide: BrowserStorageService, useValue: { getItem: vi.fn().mockReturnValue(null), setItem: vi.fn() } },
        ],
    }).compileComponents();
    fixture = TestBed.createComponent(RecipeRecognitionComponent);
    fixture.detectChanges();
});

describe('RecipeRecognitionComponent source selection', () => {
    it('visibly selects video speech for supported YouTube links and waits for Recognize before submitting', async () => {
        const recognized: RecipeImportResult[] = [];
        fixture.componentInstance.draftRecognized.subscribe(value => recognized.push(value));
        const urls = [
            reportedUrl,
            'https://www.youtube.com/shorts/ncD_3gaRKbM',
            'https://youtu.be/ncD_3gaRKbM?si=share',
            'https://m.youtube.com/watch?v=ncD_3gaRKbM',
        ];

        for (const sourceUrl of urls) {
            ai.importRecipe.mockClear();
            ai.importRecipeVideo.mockClear();
            setInputs(sourceUrl);

            expect(videoCheckbox().checked, sourceUrl).toBe(true);
            expect(ai.importRecipe).not.toHaveBeenCalled();
            expect(ai.importRecipeVideo).not.toHaveBeenCalled();

            await recognizeAsync();

            expect(ai.importRecipeVideo).toHaveBeenCalledExactlyOnceWith({ sourceUrl, text: null }, null);
            expect(ai.importRecipe).not.toHaveBeenCalled();
        }
        expect(recognized).toEqual(urls.map(() => draft));
    });

    it('uses pasted recipe text before YouTube speech until the user explicitly selects video', async () => {
        setInputs(` ${reportedUrl} `, ` ${caption} `);
        expect(videoCheckbox().checked).toBe(false);

        await recognizeAsync();

        expect(ai.importRecipe).toHaveBeenCalledExactlyOnceWith({ sourceUrl: reportedUrl, text: caption });
        expect(ai.importRecipeVideo).not.toHaveBeenCalled();

        setInputs(reportedUrl);
        expect(videoCheckbox().checked).toBe(true);
        setInputs(reportedUrl, caption);
        expect(videoCheckbox().checked).toBe(false);
    });
});

describe('RecipeRecognitionComponent manual video selection', () => {
    it('keeps a manual opt-out for the same URL through text fallback and a later retry', async () => {
        setInputs(reportedUrl);
        setVideoEnabled(false);
        setInputs(reportedUrl, caption);

        await recognizeAsync();

        expect(ai.importRecipe).toHaveBeenCalledExactlyOnceWith({ sourceUrl: reportedUrl, text: caption });
        expect(ai.importRecipeVideo).not.toHaveBeenCalled();

        setInputs(reportedUrl);
        expect(videoCheckbox().checked).toBe(false);
        await recognizeAsync();

        expect(ai.importRecipe).toHaveBeenLastCalledWith({ sourceUrl: reportedUrl, text: null });
        expect(ai.importRecipeVideo).not.toHaveBeenCalled();
    });

    it('resets the manual opt-out when a different source URL is pasted', async () => {
        setInputs(reportedUrl);
        setVideoEnabled(false);
        const nextUrl = 'https://www.youtube.com/shorts/yNgjOzU3QaU';
        setInputs(nextUrl);

        expect(videoCheckbox().checked).toBe(true);
        await recognizeAsync();
        expect(ai.importRecipeVideo).toHaveBeenCalledExactlyOnceWith({ sourceUrl: nextUrl, text: null }, null);

        setInputs(reportedUrl);
        expect(videoCheckbox().checked).toBe(true);
    });

    it('preserves explicit video selection when the caption or source URL changes', async () => {
        setInputs(reportedUrl, caption);
        setVideoEnabled(true);
        const sourceUrl = 'https://example.org/recipe.mp4';
        setInputs(sourceUrl, `${caption} Add a pinch of salt.`);

        expect(videoCheckbox().checked).toBe(true);
        await recognizeAsync();

        expect(ai.importRecipeVideo).toHaveBeenCalledExactlyOnceWith({ sourceUrl, text: `${caption} Add a pinch of salt.` }, null);
        expect(ai.importRecipe).not.toHaveBeenCalled();
    });
});

describe('RecipeRecognitionComponent other inputs', () => {
    it('keeps ordinary, invalid and lookalike URLs on the ordinary import route', async () => {
        const urls = [
            'https://example.org/recipe',
            'http://www.youtube.com/watch?v=ncD_3gaRKbM',
            'https://www.youtube.com.evil.test/watch?v=ncD_3gaRKbM',
            'https://user@www.youtube.com/watch?v=ncD_3gaRKbM',
            'https://www.youtube.com:444/watch?v=ncD_3gaRKbM',
            'https://www.youtube.com/@recipe-channel',
            'https://www.youtube.com/watch?v=invalid',
            'https://www.youtube.com/watch',
        ];

        for (const sourceUrl of urls) {
            ai.importRecipe.mockClear();
            setInputs(sourceUrl);

            expect(videoCheckbox().checked, sourceUrl).toBe(false);
            await recognizeAsync();
            expect(ai.importRecipe).toHaveBeenCalledExactlyOnceWith({ sourceUrl, text: null });
        }
        expect(ai.importRecipeVideo).not.toHaveBeenCalled();
    });

    it('submits a selected file in explicit video mode and clears it when video is turned off', async () => {
        const sourceUrl = 'https://example.org/recipe';
        setInputs(sourceUrl, caption);
        setVideoEnabled(true);
        const file = new File(['video'], 'recipe.mp4', { type: 'video/mp4' });
        const fileInput = inputElement('fd-recipe-video-input input[type="file"]');
        Object.defineProperty(fileInput, 'files', { value: { item: () => file } });
        fileInput.dispatchEvent(new Event('change', { bubbles: true }));
        fixture.detectChanges();

        await recognizeAsync();

        expect(ai.importRecipeVideo).toHaveBeenCalledExactlyOnceWith({ sourceUrl, text: caption }, file);
        setVideoEnabled(false);
        expect((fixture.nativeElement as HTMLElement).querySelector('input[type="file"]')).toBeNull();
        await recognizeAsync();

        expect(ai.importRecipe).toHaveBeenCalledExactlyOnceWith({ sourceUrl, text: caption });
        expect(ai.importRecipeVideo).toHaveBeenCalledOnce();
    });
});

function setInputs(sourceUrl: string, text = ''): void {
    fixture.componentRef.setInput('sourceUrl', sourceUrl);
    fixture.componentRef.setInput('text', text);
    fixture.detectChanges();
}

function videoCheckbox(): HTMLInputElement {
    return inputElement('fd-recipe-video-input input[type="checkbox"]');
}

function setVideoEnabled(enabled: boolean): void {
    const checkbox = videoCheckbox();
    checkbox.checked = enabled;
    checkbox.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();
}

async function recognizeAsync(): Promise<void> {
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button');
    const button = Array.from(buttons).find(control => control.textContent.includes('RECIPE_MANAGE.IMPORT.RECOGNIZE'));
    if (button === undefined) {
        throw new Error('Recognize recipe button is missing.');
    }
    button.click();
    await fixture.whenStable();
    fixture.detectChanges();
}

function inputElement(selector: string): HTMLInputElement {
    const result = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(selector);
    if (result === null) {
        throw new Error(`Missing recipe recognition control: ${selector}`);
    }
    return result;
}
