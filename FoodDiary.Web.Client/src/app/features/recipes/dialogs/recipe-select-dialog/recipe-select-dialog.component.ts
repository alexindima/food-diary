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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, type Observable, of, switchMap, tap } from 'rxjs';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { RecipeService } from '../../api/recipe.service';
import { resolveRecipeImageUrl } from '../../lib/recipe-image.util';
import type { Recipe, RecipeFilters } from '../../models/recipe.data';
import {
    RECIPE_SELECT_DIALOG_FIRST_PAGE,
    RECIPE_SELECT_DIALOG_NEXT_PAGE_OFFSET,
    RECIPE_SELECT_DIALOG_PAGE_SIZE,
} from './recipe-select-dialog.config';
import type { RecipeSelectItemViewModel } from './recipe-select-dialog.types';
import { RecipeSelectDialogContentComponent } from './recipe-select-dialog-content.component';

@Component({
    selector: 'fd-recipe-select-dialog',
    standalone: true,
    templateUrl: './recipe-select-dialog.component.html',
    styleUrls: ['./recipe-select-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        FdUiInputComponent,
        RecipeSelectDialogContentComponent,
    ],
})
export class RecipeSelectDialogComponent {
    private readonly recipeService = inject(RecipeService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);
    private readonly dialogRef = inject(FdUiDialogRef<RecipeSelectDialogComponent, Recipe | null>, {
        optional: true,
    });

    public readonly embedded = input<boolean>(false);
    public readonly recipeSelected = output<Recipe>();
    public readonly createRecipeRequested = output();
    public readonly searchValue = signal<string | null>(null);
    public readonly onlyMineFilter = signal(false);
    public readonly searchSuffixIcon = computed(() => {
        const search = this.searchValue();
        return search !== null && search.length > 0 ? 'close' : undefined;
    });
    public readonly filterIcon = computed(() => (this.onlyMineFilter() ? 'person' : 'groups'));
    protected readonly recipeItems = computed<RecipeSelectItemViewModel[]>(() =>
        this.recipeData.items().map(recipe => ({
            recipe,
            imageUrl: this.resolveImage(recipe),
        })),
    );

    public readonly searchForm = new FormGroup<RecipeSearchFormGroup>({
        search: new FormControl<string | null>(null),
        onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
    });

    public recipeData: PagedData<Recipe> = new PagedData<Recipe>();
    public currentPageIndex = 0;
    protected readonly pageSize = RECIPE_SELECT_DIALOG_PAGE_SIZE;

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.loadRecipes(RECIPE_SELECT_DIALOG_FIRST_PAGE).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                tap(value => {
                    this.searchValue.set(value);
                }),
                debounceTime(this.searchDebounceMs),
                switchMap(() => this.loadRecipes(RECIPE_SELECT_DIALOG_FIRST_PAGE)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                distinctUntilChanged(),
                tap(value => {
                    this.onlyMineFilter.set(value);
                }),
                switchMap(() => this.loadRecipes(RECIPE_SELECT_DIALOG_FIRST_PAGE)),
            )
            .subscribe();
    }

    public loadRecipes(page: number): Observable<void> {
        this.recipeData.setLoading(true);
        const includePublic = !this.searchForm.controls.onlyMine.value;
        const filters: RecipeFilters = {
            search: this.searchForm.controls.search.value ?? undefined,
        };

        return this.recipeService.query(page, RECIPE_SELECT_DIALOG_PAGE_SIZE, filters, includePublic).pipe(
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

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadRecipes(pageIndex + RECIPE_SELECT_DIALOG_NEXT_PAGE_OFFSET).subscribe();
    }

    public onRecipeClick(recipe: Recipe): void {
        this.handleSelection(recipe);
    }

    public clearSearch(): void {
        this.searchForm.controls.search.setValue('');
    }

    public toggleOnlyMine(): void {
        this.searchForm.controls.onlyMine.setValue(!this.onlyMineFilter());
    }

    private resolveImage(recipe: Recipe): string | undefined {
        return resolveRecipeImageUrl(recipe.imageUrl ?? undefined);
    }

    public onCreateRecipeClick(): void {
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

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
