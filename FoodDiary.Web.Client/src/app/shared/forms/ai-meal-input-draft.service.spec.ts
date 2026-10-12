import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { SessionEventsService } from '../auth/session-events.service';
import { MS_PER_HOUR } from '../lib/time.constants';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { AiMealInputDraftService } from './ai-meal-input-draft.service';

const DRAFT_KEY = 'ai-meal-input-premium-draft';

describe('Premium meal input round trip', () => {
    beforeEach(() => {
        TestBed.inject(BrowserStorageService).removeItem('session', DRAFT_KEY);
    });
    afterEach(() => vi.useRealTimers());

    it('restores exact text after reload only on its original route and consumes it once', () => {
        TestBed.inject(AiMealInputDraftService).save('user-1', '/dashboard', '  Два яйца\nи чай  ');
        TestBed.resetTestingModule();
        const drafts = TestBed.inject(AiMealInputDraftService);
        expect(drafts.take('user-1', '/meals')).toBe('');
        expect(drafts.take('user-1', '/dashboard')).toBe('  Два яйца\nи чай  ');
        expect(drafts.take('user-1', '/dashboard')).toBe('');
    });

    it('does not restore another account description and clears the draft on logout', () => {
        const drafts = TestBed.inject(AiMealInputDraftService);
        drafts.save('user-1', '/dashboard', 'Private description');
        expect(drafts.take('user-2', '/dashboard')).toBe('');
        drafts.save('user-1', '/dashboard', 'New description');
        TestBed.inject(SessionEventsService).notifySessionEnded();
        expect(drafts.take('user-1', '/dashboard')).toBe('');
    });

    it('expires abandoned drafts and ignores malformed stored values', () => {
        vi.useFakeTimers();
        const drafts = TestBed.inject(AiMealInputDraftService);
        drafts.save('user-1', '/dashboard', 'Eggs');
        vi.advanceTimersByTime(2 * MS_PER_HOUR);
        expect(drafts.take('user-1', '/dashboard')).toBe('');
        TestBed.inject(BrowserStorageService).setJson('session', DRAFT_KEY, {
            userId: 'user-1',
            path: '/dashboard',
            savedAt: 'invalid',
            text: 'Old text',
        });
        expect(drafts.take('user-1', '/dashboard')).toBe('');
    });
});
