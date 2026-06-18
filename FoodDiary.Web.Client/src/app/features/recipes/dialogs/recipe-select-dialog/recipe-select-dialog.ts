import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    type ElementRef,
    inject,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, type Observable, of, skip, switchMap, tap } from 'rxjs';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { resolveRecipeImageUrl } from '../../lib/recipe-image.util';
import { RecipeSelectFacade } from '../../lib/recipe-select.facade';
import type { Recipe, RecipeFilters } from '../../models/recipe.data';
import { RecipeSelectDialogContentComponent } from './recipe-select-dialog-content/recipe-select-dialog-content';
import {
    RECIPE_SELECT_DIALOG_FIRST_PAGE,
    RECIPE_SELECT_DIALOG_NEXT_PAGE_OFFSET,
    RECIPE_SELECT_DIALOG_PAGE_SIZE,
} from './recipe-select-dialog-lib/recipe-select-dialog.config';
import type { RecipeSelectItemViewModel } from './recipe-select-dialog-lib/recipe-select-dialog.types';

@Component({
    selector: 'fd-recipe-select-dialog',
    templateUrl: './recipe-select-dialog.html',
    styleUrls: ['./recipe-select-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        FdUiInputComponent,
        RecipeSelectDialogContentComponent,
    ],
})
export class RecipeSelectDialogComponent {
    private readonly recipeSelectFacade = inject(RecipeSelectFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);
    private readonly dialogRef = inject(FdUiDialogRef<RecipeSelectDialogComponent, Recipe | null>, {
        optional: true,
    });

    public readonly embedded = input<boolean>(false);
    public readonly excludedRecipeId = input<string | null>(null);
    public readonly recipeSelected = output<Recipe>();
    public readonly createRecipeRequested = output();
    protected readonly searchModel = signal<RecipeSearchFormValues>({
        search: null,
        onlyMine: false,
    });
    protected readonly searchForm = form(this.searchModel);
    protected readonly searchValue = computed(() => this.searchModel().search);
    protected readonly onlyMineFilter = computed(() => this.searchModel().onlyMine);
    protected readonly searchSuffixIcon = computed(() => {
        const search = this.searchValue();
        return search !== null && search.length > 0 ? 'close' : undefined;
    });
    protected readonly filterIcon = computed(() => (this.onlyMineFilter() ? 'person' : 'groups'));
    protected readonly recipeItems = computed<RecipeSelectItemViewModel[]>(() =>
        this.recipeData
            .items()
            .filter(recipe => recipe.id !== this.excludedRecipeId())
            .map(recipe => ({
                recipe,
                imageUrl: this.resolveImage(recipe),
            })),
    );

    protected recipeData: PagedData<Recipe> = new PagedData<Recipe>();
    protected currentPageIndex = 0;
    protected readonly pageSize = RECIPE_SELECT_DIALOG_PAGE_SIZE;

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.loadRecipes(RECIPE_SELECT_DIALOG_FIRST_PAGE).subscribe();

        toObservable(this.searchValue)
            .pipe(
                skip(1),
                takeUntilDestroyed(this.destroyRef),
                debounceTime(this.searchDebounceMs),
                switchMap(() => this.loadRecipes(RECIPE_SELECT_DIALOG_FIRST_PAGE)),
            )
            .subscribe();

        toObservable(this.onlyMineFilter)
            .pipe(
                skip(1),
                takeUntilDestroyed(this.destroyRef),
                distinctUntilChanged(),
                switchMap(() => this.loadRecipes(RECIPE_SELECT_DIALOG_FIRST_PAGE)),
            )
            .subscribe();
    }

    protected loadRecipes(page: number): Observable<void> {
        this.recipeData.setLoading(true);
        const includePublic = !this.onlyMineFilter();
        const filters: RecipeFilters = {
            search: this.searchValue() ?? undefined,
        };

        return this.recipeSelectFacade.query(page, RECIPE_SELECT_DIALOG_PAGE_SIZE, filters, includePublic).pipe(
            tap(pageData => {
                this.recipeData.setData(pageData);
                this.currentPageIndex = pageData.page - 1;
            }),
            map(() => void 0),
            catchError(() => {
                this.recipeData.clearData();
                return of(void 0);
            }),
            finalize(() => {
                this.recipeData.setLoading(false);
            }),
        );
    }

    protected onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadRecipes(pageIndex + RECIPE_SELECT_DIALOG_NEXT_PAGE_OFFSET).subscribe();
    }

    protected onRecipeClick(recipe: Recipe): void {
        this.handleSelection(recipe);
    }

    protected clearSearch(): void {
        this.searchForm.search().value.set('');
    }

    protected toggleOnlyMine(): void {
        this.searchForm.onlyMine().value.set(!this.onlyMineFilter());
    }

    private resolveImage(recipe: Recipe): string | undefined {
        return resolveRecipeImageUrl(recipe.imageUrl ?? undefined);
    }

    protected onCreateRecipeClick(): void {
        this.createRecipeRequested.emit();
    }

    private handleSelection(recipe: Recipe): void {
        if (!this.embedded() && this.dialogRef !== null) {
            this.dialogRef.close(recipe);
        } else {
            this.recipeSelected.emit(recipe);
        }
    }

    private scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

type RecipeSearchFormValues = {
    search: string | null;
    onlyMine: boolean;
};
