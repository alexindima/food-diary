import { DestroyRef, inject, Injectable, signal } from '@angular/core';

import { BrowserClipboardService } from '../../../shared/platform/browser-clipboard.service';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';

const preferenceKey = 'recipe.clipboardSuggestions';
const dismissedKey = 'recipe.dismissedClipboardUrl';
const maxClipboardUrlLength = 2048;

export function instagramRecipeUrl(text: string): string | null {
    if (text.length > maxClipboardUrlLength) {
        return null;
    }
    try {
        let url = new URL(text.trim());
        if (url.hostname === 'vk.ru' && url.pathname === '/away.php') {
            url = new URL(url.searchParams.get('to') ?? '');
        }
        if (!hasInstagramAuthority(url) || !/^\/(reel|p|tv)\/[\w-]+\/?$/.test(url.pathname)) {
            return null;
        }
        return `https://www.instagram.com${url.pathname.replace(/\/$/, '')}/`;
    } catch {
        return null;
    }
}

function hasInstagramAuthority(url: URL): boolean {
    return (
        url.protocol === 'https:' &&
        url.username === '' &&
        url.password === '' &&
        url.port === '' &&
        ['instagram.com', 'www.instagram.com'].includes(url.hostname)
    );
}

// eslint-disable-next-line @angular-eslint/use-injectable-provided-in -- State belongs to the recipe import component and is destroyed with it.
@Injectable()
export class RecipeClipboardSuggestion {
    private readonly clipboard = inject(BrowserClipboardService);
    private readonly storage = inject(BrowserStorageService);
    private reading = false;
    public readonly enabled = signal(this.storage.getItem('local', preferenceKey) === 'true');
    public readonly suggestion = signal<string | null>(null);
    public readonly unavailable = signal(false);

    public constructor() {
        const stop = this.clipboard.onReturn(() => {
            void this.checkOnReturnAsync();
        });
        inject(DestroyRef).onDestroy(stop);
    }

    public async setEnabledAsync(enabled: boolean): Promise<void> {
        this.enabled.set(enabled);
        this.storage.setItem('local', preferenceKey, String(enabled));
        this.suggestion.set(null);
        if (enabled) {
            // This read follows the user's click, allowing browser permission UI if needed.
            await this.readSuggestionAsync();
        }
    }

    public dismiss(): void {
        const url = this.suggestion();
        if (url !== null) {
            this.storage.setItem('session', dismissedKey, url);
        }
        this.suggestion.set(null);
    }

    public async pasteAsync(): Promise<string | null> {
        try {
            const text = await this.clipboard.readTextAsync();
            this.unavailable.set(false);
            return text.length <= maxClipboardUrlLength ? text.trim() : null;
        } catch {
            this.unavailable.set(true);
            return null;
        }
    }

    private async checkOnReturnAsync(): Promise<void> {
        if (this.enabled() && (await this.clipboard.canReadWithoutPromptAsync())) {
            await this.readSuggestionAsync();
        }
    }

    private async readSuggestionAsync(): Promise<void> {
        if (this.reading) {
            return;
        }
        this.reading = true;
        try {
            const text = await this.clipboard.readTextAsync();
            const url = instagramRecipeUrl(text);
            this.unavailable.set(false);
            if (this.enabled()) {
                this.suggestion.set(url !== this.storage.getItem('session', dismissedKey) ? url : null);
            }
        } catch {
            this.unavailable.set(true);
        } finally {
            this.reading = false;
        }
    }
}
