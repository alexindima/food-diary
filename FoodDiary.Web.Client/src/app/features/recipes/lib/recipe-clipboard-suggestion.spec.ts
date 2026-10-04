import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { BrowserClipboardService } from '../../../shared/platform/browser-clipboard.service';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';
import { instagramRecipeUrl, RecipeClipboardSuggestion } from './recipe-clipboard-suggestion';

const link = 'https://www.instagram.com/reel/DdwiuWSNL5o/';
let onReturn: () => void;
let suggestion: RecipeClipboardSuggestion;
const clipboard = { readTextAsync: vi.fn(), canReadWithoutPromptAsync: vi.fn(), onReturn: vi.fn() };

beforeEach(() => {
    clipboard.readTextAsync.mockReset().mockResolvedValue(`${link}?stkn=tracking`);
    clipboard.canReadWithoutPromptAsync.mockReset().mockResolvedValue(true);
    clipboard.onReturn.mockImplementation((callback: () => void) => {
        onReturn = callback;
        return vi.fn();
    });
    const values = new Map<string, string>();
    TestBed.configureTestingModule({
        providers: [
            RecipeClipboardSuggestion,
            { provide: BrowserClipboardService, useValue: clipboard },
            {
                provide: BrowserStorageService,
                useValue: {
                    getItem: (scope: string, key: string): string | null => values.get(scope + key) ?? null,
                    setItem: (scope: string, key: string, value: string): void => {
                        values.set(scope + key, value);
                    },
                },
            },
        ],
    });
    suggestion = TestBed.inject(RecipeClipboardSuggestion);
});

describe('clipboard suggestions', () => {
    it('does not read until opted in', async () => {
        onReturn();
        await waitForAsyncTasksAsync();
        expect(clipboard.readTextAsync).not.toHaveBeenCalled();
        expect(suggestion.suggestion()).toBeNull();
    });
    it('offers canonical URLs and does not repeat a dismissed link', async () => {
        await suggestion.setEnabledAsync(true);
        expect(suggestion.suggestion()).toBe(link);
        suggestion.dismiss();
        onReturn();
        await waitForAsyncTasksAsync();
        expect(suggestion.suggestion()).toBeNull();
    });
    it('does not request permission on return if it is not granted', async () => {
        await suggestion.setEnabledAsync(true);
        clipboard.readTextAsync.mockClear();
        clipboard.canReadWithoutPromptAsync.mockResolvedValue(false);
        onReturn();
        await waitForAsyncTasksAsync();
        expect(clipboard.readTextAsync).not.toHaveBeenCalled();
    });
    it('offers manual paste fallback on denial', async () => {
        clipboard.readTextAsync.mockRejectedValue(new Error('NotAllowedError'));
        await suggestion.setEnabledAsync(true);
        expect(suggestion.unavailable()).toBe(true);
        expect(suggestion.suggestion()).toBeNull();
    });
});

describe('Instagram link detection', () => {
    it('unwraps VK links', () => {
        expect(instagramRecipeUrl(`https://vk.ru/away.php?to=${encodeURIComponent(link)}`)).toBe(link);
    });
    it.each([
        'https://instagram.com.attacker.org/reel/example/',
        'https://user:pw@instagram.com/reel/example/',
        'https://instagram.com:8080/reel/example/',
        'http://instagram.com/reel/example/',
        'unrelated private clipboard text',
    ])('ignores unsupported content %s', text => {
        expect(instagramRecipeUrl(text)).toBeNull();
    });
});
