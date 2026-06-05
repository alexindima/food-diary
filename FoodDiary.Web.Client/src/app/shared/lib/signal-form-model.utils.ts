import type { WritableSignal } from '@angular/core';

/**
 * Patches a signal form model while preserving the current object reference when no values change.
 * `undefined` patch values are treated as "leave the current field untouched".
 */
export function patchSignalFormModel<T extends object>(model: WritableSignal<T>, patch: Partial<T>): void {
    model.update(current => {
        const changedPatch: Partial<T> = {};
        let hasChanges = false;

        for (const key in patch) {
            if (!Object.hasOwn(patch, key)) {
                continue;
            }

            const value = patch[key];
            if (value === undefined || current[key] === value) {
                continue;
            }

            changedPatch[key] = value;
            hasChanges = true;
        }

        return hasChanges
            ? {
                  ...current,
                  ...changedPatch,
              }
            : current;
    });
}
