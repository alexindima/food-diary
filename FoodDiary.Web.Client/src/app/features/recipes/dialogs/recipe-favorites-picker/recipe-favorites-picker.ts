import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogShellComponent, FdUiInputComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { fdUiCoerceInputTextValue, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import { debounceTime, distinctUntilChanged, filter, map, type Observable, of, Subject, switchMap, take } from 'rxjs';

import type { FavoriteRecipe } from '../../../../shared/models/recipe.data';
import { FavoritesPickerActions } from '../../../../shared/ui/favorites/favorites-picker-actions';
import { FavoriteRecipeRowComponent } from '../../components/favorite-recipe-row/favorite-recipe-row';
import { RecipeFavoritesPickerFacade } from '../../lib/favorites/recipe-favorites-picker.facade';

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
    private readonly data = inject<RecipeFavoritesPickerData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<RecipeFavoritesPickerComponent, boolean>);
    private readonly destroyRef = inject(DestroyRef);
    private readonly actions = new FavoritesPickerActions<FavoriteRecipe>({
        page: this.facade,
        remove: (item): Observable<boolean> => this.data.remove(item),
        restore: (item): Observable<boolean> => this.data.restore(item),
        repeat: (item): Observable<boolean> => this.data.repeat(item),
        added: (): void => {
            this.ref.close(true);
        },
        destroyRef: this.destroyRef,
    });
    protected readonly savingId = this.actions.savingId;
    protected readonly removingId = this.actions.removingId;
    protected readonly removeFailed = this.actions.removeFailed;
    protected readonly restoringId = this.actions.restoringId;
    protected readonly restoreErrors = this.actions.restoreErrors;
    protected readonly busy = this.actions.busy;
    protected readonly focusTarget = this.actions.focusTarget;
    protected readonly saveFailed = this.actions.saveFailed;
    protected readonly operationErrorKey = computed(() => {
        if (this.saveFailed()) {
            return 'RECIPE_FAVORITES.ADD_ERROR';
        }
        return this.removeFailed() ? 'RECIPE_FAVORITES.REMOVE_ERROR' : null;
    });

    private readonly searches = new Subject<string>();

    public constructor() {
        this.facade.load();
        const busyChanges = toObservable(this.busy);
        this.searches
            .pipe(
                map(value => value.trim()),
                debounceTime(SEARCH_DEBOUNCE_MS),
                distinctUntilChanged(),
                // A queued search must observe the mutation that disabled the search field.
                switchMap(search =>
                    this.busy()
                        ? busyChanges.pipe(
                              filter(() => !this.busy()),
                              take(1),
                              map(() => search),
                          )
                        : of(search),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(search => {
                this.load(1, search);
            });
    }

    protected search(value: FdUiInputValue): void {
        this.searches.next(fdUiCoerceInputTextValue(value));
    }

    protected load(page: number, search = this.facade.search()): void {
        this.actions.resetPresentation();
        this.facade.load(page, search);
    }

    protected changePage(page: number): void {
        if (!this.busy()) {
            this.load(page);
        }
    }

    protected remove(item: FavoriteRecipe): void {
        this.actions.remove(item);
    }

    protected undoRemoval(item: FavoriteRecipe): void {
        this.actions.restore(item);
    }

    protected add(item: FavoriteRecipe): void {
        this.actions.add(item);
    }
}
