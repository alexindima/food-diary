import { afterRenderEffect, ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal, viewChildren } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogShellComponent, FdUiInputComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { debounceTime, distinctUntilChanged, finalize, map, type Observable, Subject } from 'rxjs';

import { FavoriteMealRowComponent } from '../../components/favorite-meal-row/favorite-meal-row';
import { MealFavoritesPickerFacade } from '../../lib/favorites/meal-favorites-picker.facade';
import type { FavoriteMeal } from '../../models/meal.data';

const SEARCH_DEBOUNCE_MS = 300;

export type MealFavoritesPickerData = {
    repeat: (favorite: FavoriteMeal) => Observable<boolean>;
    remove: (favorite: FavoriteMeal) => Observable<boolean>;
    restore: (favorite: FavoriteMeal) => Observable<boolean>;
};

@Component({
    selector: 'fd-meal-favorites-picker',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogShellComponent,
        FdUiInputComponent,
        FdUiPaginationComponent,
        FavoriteMealRowComponent,
    ],
    templateUrl: './meal-favorites-picker.html',
    styleUrl: './meal-favorites-picker.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [MealFavoritesPickerFacade],
})
export class MealFavoritesPickerComponent {
    protected readonly facade = inject(MealFavoritesPickerFacade);
    protected readonly savingId = signal<string | null>(null);
    protected readonly removingId = signal<string | null>(null);
    protected readonly removeFailed = signal(false);
    protected readonly restoringId = signal<string | null>(null);
    protected readonly restoreErrors = signal<ReadonlySet<string>>(new Set());
    protected readonly busy = computed(() => this.savingId() !== null || this.removingId() !== null || this.restoringId() !== null);
    private readonly rows = viewChildren(FavoriteMealRowComponent);
    private readonly focusTarget = signal<string | null>(null);
    protected readonly saveFailed = signal(false);
    protected readonly operationErrorKey = computed(() => {
        if (this.saveFailed()) {
            return 'MEAL_FAVORITES.ADD_ERROR';
        }
        return this.removeFailed() ? 'MEAL_FAVORITES.REMOVE_ERROR' : null;
    });
    private readonly data = inject<MealFavoritesPickerData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<MealFavoritesPickerComponent, boolean>);
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
                .find(row => row.meal().id === target)
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

    protected remove(item: FavoriteMeal): void {
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

    protected undoRemoval(item: FavoriteMeal): void {
        if (!this.facade.removedIds().has(item.id) || this.busy()) {
            return;
        }
        const revision = this.facade.revision();
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
                        this.facade.markRestored(item.id);
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

    protected add(item: FavoriteMeal): void {
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
