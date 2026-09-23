import { afterRenderEffect, ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal, viewChildren } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogShellComponent, FdUiInputComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { debounceTime, distinctUntilChanged, finalize, map, type Observable, Subject } from 'rxjs';

import { FavoriteRecipeRowComponent } from '../../components/favorite-recipe-row/favorite-recipe-row';
import { RecipeFavoritesPickerFacade } from '../../lib/favorites/recipe-favorites-picker.facade';
import type { FavoriteRecipe } from '../../models/recipe.data';

const SEARCH_DEBOUNCE_MS = 300;

export type RecipeFavoritesPickerData = {
    repeat: (favorite: FavoriteRecipe) => Observable<boolean>;
    remove: (favorite: FavoriteRecipe) => Observable<boolean>;
    restore: (favorite: FavoriteRecipe) => Observable<boolean>;
};

@Component({
    selector: 'fd-recipe-favorites-picker',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogShellComponent,
        FdUiInputComponent,
        FdUiPaginationComponent,
        FavoriteRecipeRowComponent,
    ],
    templateUrl: './recipe-favorites-picker.html',
    styleUrl: './recipe-favorites-picker.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RecipeFavoritesPickerFacade],
})
export class RecipeFavoritesPickerComponent {
    protected readonly facade = inject(RecipeFavoritesPickerFacade);
    protected readonly savingId = signal<string | null>(null);
    protected readonly removingId = signal<string | null>(null);
    protected readonly removeFailed = signal(false);
    protected readonly restoringId = signal<string | null>(null);
    protected readonly restoreErrors = signal<ReadonlySet<string>>(new Set());
    protected readonly busy = computed(() => this.savingId() !== null || this.removingId() !== null || this.restoringId() !== null);
    private readonly rows = viewChildren(FavoriteRecipeRowComponent);
    private readonly focusTarget = signal<string | null>(null);
    protected readonly saveFailed = signal(false);
    protected readonly operationErrorKey = computed(() => {
        if (this.saveFailed()) {
            return 'RECIPE_FAVORITES.ADD_ERROR';
        }
        return this.removeFailed() ? 'RECIPE_FAVORITES.REMOVE_ERROR' : null;
    });
    private readonly data = inject<RecipeFavoritesPickerData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<RecipeFavoritesPickerComponent, boolean>);
    private readonly destroyRef = inject(DestroyRef);
    private readonly searches = new Subject<string>();

    public constructor() {
        this.facade.load();
        afterRenderEffect(() => {
            const target = this.focusTarget();
            if (target === null || this.busy()) {
                return;
            }
            this.rows()
                .find(row => row.recipe().id === target)
                ?.action()
                ?.nativeElement.querySelector<HTMLButtonElement>('button')
                ?.focus();
            this.focusTarget.set(null);
        });
        this.searches
            .pipe(
                map(value => value.trim()),
                debounceTime(SEARCH_DEBOUNCE_MS),
                distinctUntilChanged(),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(search => {
                this.load(1, search);
            });
    }

    protected search(value: string | number | null): void {
        this.searches.next(String(value ?? ''));
    }

    protected load(page: number, search = this.facade.search()): void {
        this.restoreErrors.set(new Set());
        this.removeFailed.set(false);
        this.saveFailed.set(false);
        this.focusTarget.set(null);
        this.facade.load(page, search);
    }

    protected changePage(page: number): void {
        if (!this.busy()) {
            this.load(page);
        }
    }

    protected remove(item: FavoriteRecipe): void {
        if (this.busy() || this.facade.removedIds().has(item.id)) {
            return;
        }
        this.removingId.set(item.id);
        this.removeFailed.set(false);
        const revision = this.facade.revision();
        this.data
            .remove(item)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.removingId.set(null);
                }),
            )
            .subscribe({
                next: removed => {
                    if (revision !== this.facade.revision()) {
                        return;
                    }
                    if (removed) {
                        this.focusTarget.set(item.id);
                        this.facade.markRemoved(item.id);
                    } else {
                        this.removeFailed.set(true);
                    }
                },
                error: () => {
                    if (revision === this.facade.revision()) {
                        this.removeFailed.set(true);
                    }
                },
            });
    }

    protected undoRemoval(item: FavoriteRecipe): void {
        if (!this.facade.removedIds().has(item.id) || this.busy()) {
            return;
        }
        const revision = this.facade.revision();
        const removedId = item.id;
        this.restoringId.set(item.id);
        this.restoreErrors.update(ids => new Set([...ids].filter(id => id !== item.id)));
        this.data
            .restore(item)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.restoringId.set(null);
                }),
            )
            .subscribe({
                next: restored => {
                    if (revision !== this.facade.revision()) {
                        return;
                    }
                    if (restored) {
                        this.facade.markRestored(removedId);
                        this.focusTarget.set(item.id);
                    } else {
                        this.restoreErrors.update(ids => new Set([...ids, item.id]));
                    }
                },
                error: () => {
                    if (revision === this.facade.revision()) {
                        this.restoreErrors.update(ids => new Set([...ids, item.id]));
                    }
                },
            });
    }

    protected add(item: FavoriteRecipe): void {
        if (this.busy()) {
            return;
        }
        this.savingId.set(item.id);
        this.saveFailed.set(false);
        this.data
            .repeat(item)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.savingId.set(null);
                }),
            )
            .subscribe({
                next: added => {
                    if (added) {
                        this.ref.close(true);
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
