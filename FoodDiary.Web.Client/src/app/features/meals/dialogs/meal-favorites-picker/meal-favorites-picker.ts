import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogShellComponent, FdUiInputComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { debounceTime, distinctUntilChanged, map, type Observable, Subject } from 'rxjs';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { type MealTypeOption, normalizeMealType, resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import type { FavoriteMeal } from '../../../../shared/models/meal.data';
import { FavoritesPickerActions } from '../../../../shared/ui/favorites/favorites-picker-actions';
import { FavoriteMealRowComponent } from '../../components/favorite-meal-row/favorite-meal-row';
import { buildMealTypeSelectOptions } from '../../components/manage/meal-manage-lib/meal-manage-options.mapper';
import { MealFavoritesPickerFacade } from '../../lib/favorites/meal-favorites-picker.facade';

const SEARCH_DEBOUNCE_MS = 300;

export type MealFavoritesPickerData = {
    repeat: (favorite: FavoriteMeal, mealType: MealTypeOption) => Observable<boolean>;
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
        FdUiSelectComponent,
        FdUiPaginationComponent,
        FavoriteMealRowComponent,
    ],
    templateUrl: './meal-favorites-picker.html',
    styleUrl: './meal-favorites-picker.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [MealFavoritesPickerFacade],
})
export class MealFavoritesPickerComponent {
    private readonly translateService = inject(TranslateService);
    private readonly language = injectCurrentLanguage();
    protected readonly selectedMealType = signal<MealTypeOption>(resolveMealTypeByTime(new Date()));
    protected readonly mealTypeOptions = computed(() => {
        this.language();
        return buildMealTypeSelectOptions(this.translateService);
    });
    protected readonly facade = inject(MealFavoritesPickerFacade);
    private readonly data = inject<MealFavoritesPickerData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<MealFavoritesPickerComponent, boolean>);
    private readonly destroyRef = inject(DestroyRef);
    private readonly actions = new FavoritesPickerActions<FavoriteMeal>({
        page: this.facade,
        remove: (item): Observable<boolean> => this.data.remove(item),
        restore: (item): Observable<boolean> => this.data.restore(item),
        repeat: (item): Observable<boolean> => this.data.repeat(item, this.selectedMealType()),
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
            return 'MEAL_FAVORITES.ADD_ERROR';
        }
        return this.removeFailed() ? 'MEAL_FAVORITES.REMOVE_ERROR' : null;
    });

    private readonly searches = new Subject<string>();

    protected changeMealType(value: string | number | null): void {
        const mealType = normalizeMealType(typeof value === 'string' ? value : null);
        if (mealType !== null) {
            this.selectedMealType.set(mealType);
        }
    }

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

    protected remove(item: FavoriteMeal): void {
        this.actions.remove(item);
    }

    protected undoRemoval(item: FavoriteMeal): void {
        this.actions.restore(item);
    }

    protected add(item: FavoriteMeal): void {
        this.actions.add(item);
    }
}
