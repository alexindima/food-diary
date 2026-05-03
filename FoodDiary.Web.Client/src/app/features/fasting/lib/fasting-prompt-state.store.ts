import { inject, Injectable } from '@angular/core';

import { BrowserStorageService } from '../../../services/browser-storage.service';

interface FastingPromptState {
    dismissed?: boolean;
    snoozedUntilUtc?: string;
}

@Injectable({
    providedIn: 'root',
})
export class FastingPromptStateStore {
    private static readonly StorageKey = 'fd_fasting_prompt_state';
    private readonly storage = inject(BrowserStorageService);

    public read(): Partial<Record<string, FastingPromptState>> {
        return this.storage.getJson<Partial<Record<string, FastingPromptState>>>('local', FastingPromptStateStore.StorageKey) ?? {};
    }

    public write(state: Partial<Record<string, FastingPromptState>>): void {
        this.storage.setJson('local', FastingPromptStateStore.StorageKey, state);
    }
}
