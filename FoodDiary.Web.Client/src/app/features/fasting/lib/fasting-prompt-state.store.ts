import { inject, Injectable } from '@angular/core';

import { BrowserStorageService } from '../../../services/browser-storage.service';
import { isRecord } from '../../../shared/lib/unknown-value.utils';

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
        const state = this.storage.getJson('local', FastingPromptStateStore.StorageKey);
        if (!isRecord(state)) {
            return {};
        }

        return Object.fromEntries(
            Object.entries(state).filter((entry): entry is [string, FastingPromptState] => this.isPromptState(entry[1])),
        );
    }

    public write(state: Partial<Record<string, FastingPromptState>>): void {
        this.storage.setJson('local', FastingPromptStateStore.StorageKey, state);
    }

    private isPromptState(value: unknown): value is FastingPromptState {
        return (
            isRecord(value) &&
            (value['dismissed'] === undefined || typeof value['dismissed'] === 'boolean') &&
            (value['snoozedUntilUtc'] === undefined || typeof value['snoozedUntilUtc'] === 'string')
        );
    }
}
