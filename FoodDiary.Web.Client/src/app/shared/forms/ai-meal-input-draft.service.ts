import { inject, Service } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SessionEventsService } from '../auth/session-events.service';
import { MS_PER_HOUR } from '../lib/time.constants';
import { getNumberProperty, getStringProperty } from '../lib/unknown-value.utils';
import { BrowserStorageService } from '../platform/browser-storage.service';

const STORAGE_KEY = 'ai-meal-input-premium-draft';
const MAX_DRAFT_AGE_MS = 2 * MS_PER_HOUR;
type InputDraft = { userId: string; path: string; text: string; savedAt: number };

/** Preserve a meal description for an account's Premium navigation round trip. */
@Service()
export class AiMealInputDraftService {
    private readonly storage = inject(BrowserStorageService);
    private draft: InputDraft | null = null;

    public constructor() {
        inject(SessionEventsService)
            .sessionEnded$.pipe(takeUntilDestroyed())
            .subscribe(() => {
                this.clear();
            });
    }

    public save(userId: string | null, path: string | null, text: string): void {
        if (userId === null || path === null || text.trim().length === 0) {
            return;
        }
        this.draft = { userId, path, text, savedAt: Date.now() };
        this.storage.setJson('session', STORAGE_KEY, this.draft);
    }

    public take(userId: string | null, path: string | null): string {
        const draft: unknown = this.draft ?? this.storage.getJson('session', STORAGE_KEY);
        const savedAt = getNumberProperty(draft, 'savedAt');
        if (getStringProperty(draft, 'userId') !== userId || !isFresh(savedAt)) {
            this.clear();
            return '';
        }
        if (getStringProperty(draft, 'path') !== path) {
            return '';
        }
        const text = getStringProperty(draft, 'text') ?? '';
        this.clear();
        return text;
    }

    private clear(): void {
        this.draft = null;
        this.storage.removeItem('session', STORAGE_KEY);
    }
}

function isFresh(savedAt: number | undefined): boolean {
    return savedAt !== undefined && Number.isFinite(savedAt) && Date.now() >= savedAt && Date.now() - savedAt < MAX_DRAFT_AGE_MS;
}
