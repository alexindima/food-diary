import { computed, type Signal, signal } from '@angular/core';

export type CollectionTouchedState = {
    readonly touched: Signal<boolean>;
    readonly error: Signal<string | null>;
    markTouched: () => void;
    reset: () => void;
};

export function createCollectionTouchedState(options: {
    hasItems: () => boolean;
    errorMessage: () => string;
    dependencies?: ReadonlyArray<() => unknown>;
}): CollectionTouchedState {
    const touched = signal(false);

    return {
        touched: computed(() => touched()),
        error: computed(() => {
            for (const dependency of options.dependencies ?? []) {
                dependency();
            }

            return touched() && !options.hasItems() ? options.errorMessage() : null;
        }),
        markTouched: (): void => {
            touched.set(true);
        },
        reset: (): void => {
            touched.set(false);
        },
    };
}
