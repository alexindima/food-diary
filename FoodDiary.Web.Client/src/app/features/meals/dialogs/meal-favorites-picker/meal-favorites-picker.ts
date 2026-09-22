import {
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    ElementRef,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogShellComponent, FdUiInputComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
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
        FdUiDialogFooterDirective,
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
    protected readonly restoring = signal(false);
    protected readonly restoreFailed = signal(false);
    protected readonly undoMessageKey = computed(() => (this.restoreFailed() ? 'MEAL_FAVORITES.RESTORE_ERROR' : 'MEAL_FAVORITES.REMOVED'));
    private readonly removedItems = signal<FavoriteMeal[]>([]);
    protected readonly undoItem = computed(() => this.removedItems().at(-1));
    protected readonly undoDescription = computed(() => {
        const name = this.undoItem()?.name?.trim() ?? '';
        return name.length > 0 ? name : (this.undoItem()?.itemNames?.join(', ') ?? '');
    });
    protected readonly busy = computed(() => this.savingId() !== null || this.removingId() !== null || this.restoring());
    private readonly undoButton = viewChild<FdUiButtonComponent, ElementRef<HTMLElement>>('undoButton', { read: ElementRef });
    private readonly searchInput = viewChild<FdUiInputComponent, ElementRef<HTMLElement>>('searchInput', { read: ElementRef });
    private readonly focusTarget = signal<'undo' | 'search' | null>(null);
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
            const host = target === 'undo' ? this.undoButton() : this.searchInput();
            host?.nativeElement.querySelector<HTMLElement>('button, input')?.focus();
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
                this.facade.load(1, search);
            });
    }

    protected search(value: string | number | null): void {
        this.searches.next(String(value ?? ''));
    }

    protected remove(item: FavoriteMeal): void {
        if (this.busy()) {
            return;
        }
        this.removingId.set(item.id);
        this.removeFailed.set(false);
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
                    if (removed) {
                        this.removedItems.update(items => [...items, item]);
                        this.restoreFailed.set(false);
                        this.focusTarget.set('undo');
                        this.facade.reloadAfterRemoval();
                    } else {
                        this.removeFailed.set(true);
                    }
                },
                error: () => {
                    this.removeFailed.set(true);
                },
            });
    }

    protected undoRemoval(): void {
        const item = this.undoItem();
        if (item === undefined || this.busy()) {
            return;
        }
        this.restoring.set(true);
        this.restoreFailed.set(false);
        this.data
            .restore(item)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.restoring.set(false);
                }),
            )
            .subscribe({
                next: restored => {
                    if (!restored) {
                        this.restoreFailed.set(true);
                        return;
                    }
                    this.removedItems.update(items => items.slice(0, -1));
                    this.focusTarget.set(this.undoItem() === undefined ? 'search' : 'undo');
                    this.facade.load(1);
                },
                error: () => {
                    this.restoreFailed.set(true);
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
