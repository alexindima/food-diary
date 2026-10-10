import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogShellComponent, FdUiInputComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { debounceTime, distinctUntilChanged, map, type Observable, Subject } from 'rxjs';

import type { FavoriteProduct } from '../../../../shared/models/product.data';
import { FavoritesPickerActions } from '../../../../shared/ui/favorites/favorites-picker-actions';
import { FavoriteProductRowComponent } from '../../components/favorite-product-row/favorite-product-row';
import { ProductFavoritesPickerFacade } from '../../lib/favorites/product-favorites-picker.facade';

const SEARCH_DEBOUNCE_MS = 300;

export type ProductFavoritesPickerData = {
    repeat: (favorite: FavoriteProduct) => Observable<boolean>;
    remove: (favorite: FavoriteProduct) => Observable<boolean>;
    restore: (favorite: FavoriteProduct) => Observable<boolean>;
};

@Component({
    selector: 'fd-product-favorites-picker',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogShellComponent,
        FdUiInputComponent,
        FdUiPaginationComponent,
        FavoriteProductRowComponent,
    ],
    templateUrl: './product-favorites-picker.html',
    styleUrl: './product-favorites-picker.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductFavoritesPickerFacade],
})
export class ProductFavoritesPickerComponent {
    protected readonly facade = inject(ProductFavoritesPickerFacade);
    private readonly data = inject<ProductFavoritesPickerData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<ProductFavoritesPickerComponent, boolean>);
    private readonly destroyRef = inject(DestroyRef);
    private readonly actions = new FavoritesPickerActions<FavoriteProduct>({
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
            return 'PRODUCT_FAVORITES.ADD_ERROR';
        }
        return this.removeFailed() ? 'PRODUCT_FAVORITES.REMOVE_ERROR' : null;
    });

    private readonly searches = new Subject<string>();

    public constructor() {
        this.facade.load();
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
        this.actions.resetPresentation();
        this.facade.load(page, search);
    }

    protected changePage(page: number): void {
        if (!this.busy()) {
            this.load(page);
        }
    }

    protected remove(item: FavoriteProduct): void {
        this.actions.remove(item);
    }

    protected undoRemoval(item: FavoriteProduct): void {
        this.actions.restore(item);
    }

    protected add(item: FavoriteProduct): void {
        this.actions.add(item);
    }
}
