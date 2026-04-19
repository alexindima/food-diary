import { Injectable, inject } from '@angular/core';
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

    public read(): Record<string, FastingPromptState> {
        return this.storage.getJson<Record<string, FastingPromptState>>('local', FastingPromptStateStore.StorageKey) ?? {};
    }

    public write(state: Record<string, FastingPromptState>): void {
        this.storage.setJson('local', FastingPromptStateStore.StorageKey, state);
    }
}
