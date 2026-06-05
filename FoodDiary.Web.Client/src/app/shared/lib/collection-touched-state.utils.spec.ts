import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { createCollectionTouchedState } from './collection-touched-state.utils';

describe('createCollectionTouchedState', () => {
    it('does not show an error before the collection is touched', () => {
        const hasItems = signal(false);
        const state = createCollectionTouchedState({
            hasItems,
            errorMessage: () => 'Required',
        });

        expect(state.touched()).toBe(false);
        expect(state.error()).toBeNull();
    });

    it('shows an error for an empty touched collection', () => {
        const hasItems = signal(false);
        const state = createCollectionTouchedState({
            hasItems,
            errorMessage: () => 'Required',
        });

        state.markTouched();

        expect(state.touched()).toBe(true);
        expect(state.error()).toBe('Required');
    });

    it('clears the error when the collection has items or resets touched state', () => {
        const hasItems = signal(false);
        const state = createCollectionTouchedState({
            hasItems,
            errorMessage: () => 'Required',
        });

        state.markTouched();
        hasItems.set(true);

        expect(state.error()).toBeNull();

        hasItems.set(false);
        state.reset();

        expect(state.touched()).toBe(false);
        expect(state.error()).toBeNull();
    });

    it('recomputes the error when a dependency changes', () => {
        const hasItems = signal(false);
        const message = signal('Required');
        const state = createCollectionTouchedState({
            hasItems,
            errorMessage: () => message(),
            dependencies: [message],
        });

        state.markTouched();
        message.set('Обязательно');

        expect(state.error()).toBe('Обязательно');
    });
});
