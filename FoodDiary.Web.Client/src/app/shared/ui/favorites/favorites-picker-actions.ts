import { computed, type DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, type Observable } from 'rxjs';

import type { FavoritesPageState } from './favorites-page-state';

type FavoritesActionOptions<T extends { id: string }> = {
    page: Pick<FavoritesPageState<T>, 'revision' | 'removedIds' | 'markRemoved' | 'markRestored'>;
    remove: (item: T) => Observable<boolean>;
    restore: (item: T) => Observable<boolean>;
    repeat: (item: T) => Observable<boolean>;
    added: () => void;
    destroyRef: DestroyRef;
};

/** Independent undo rows and revision guards shared by the three picker dialogs. */
export class FavoritesPickerActions<T extends { id: string }> {
    public readonly savingId = signal<T['id'] | null>(null);
    public readonly removingId = signal<T['id'] | null>(null);
    public readonly restoringId = signal<T['id'] | null>(null);
    public readonly removeFailed = signal(false);
    public readonly saveFailed = signal(false);
    public readonly restoreErrors = signal<ReadonlySet<T['id']>>(new Set());
    public readonly focusTarget = signal<T['id'] | null>(null);
    public readonly busy = computed(() => this.savingId() !== null || this.removingId() !== null || this.restoringId() !== null);

    public constructor(private readonly options: FavoritesActionOptions<T>) {}

    public resetPresentation(): void {
        this.restoreErrors.set(new Set());
        this.removeFailed.set(false);
        this.saveFailed.set(false);
        this.focusTarget.set(null);
    }

    public remove(item: T): void {
        const { page } = this.options;
        if (this.busy() || page.removedIds().has(item.id)) {
            return;
        }
        this.removingId.set(item.id);
        this.removeFailed.set(false);
        const revision = page.revision();
        this.options
            .remove(item)
            .pipe(
                takeUntilDestroyed(this.options.destroyRef),
                finalize(() => {
                    this.removingId.set(null);
                }),
            )
            .subscribe({
                next: removed => {
                    if (revision !== page.revision()) {
                        return;
                    }
                    if (removed) {
                        this.focusTarget.set(item.id);
                        page.markRemoved(item.id);
                    } else {
                        this.removeFailed.set(true);
                    }
                },
                error: () => {
                    if (revision === page.revision()) {
                        this.removeFailed.set(true);
                    }
                },
            });
    }

    public restore(item: T): void {
        const { page } = this.options;
        if (this.busy() || !page.removedIds().has(item.id)) {
            return;
        }
        const revision = page.revision();
        this.restoringId.set(item.id);
        this.restoreErrors.update(ids => new Set([...ids].filter(id => id !== item.id)));
        this.options
            .restore(item)
            .pipe(
                takeUntilDestroyed(this.options.destroyRef),
                finalize(() => {
                    this.restoringId.set(null);
                }),
            )
            .subscribe({
                next: restored => {
                    if (revision !== page.revision()) {
                        return;
                    }
                    if (restored) {
                        page.markRestored(item.id);
                        this.focusTarget.set(item.id);
                    } else {
                        this.restoreErrors.update(ids => new Set([...ids, item.id]));
                    }
                },
                error: () => {
                    if (revision === page.revision()) {
                        this.restoreErrors.update(ids => new Set([...ids, item.id]));
                    }
                },
            });
    }

    public add(item: T): void {
        if (this.busy()) {
            return;
        }
        this.savingId.set(item.id);
        this.saveFailed.set(false);
        this.options
            .repeat(item)
            .pipe(
                takeUntilDestroyed(this.options.destroyRef),
                finalize(() => {
                    this.savingId.set(null);
                }),
            )
            .subscribe({
                next: added => {
                    if (added) {
                        this.options.added();
                    } else {
                        this.saveFailed.set(true);
                    }
                },
                error: () => {
                    this.saveFailed.set(true);
                },
            });
    }
}
