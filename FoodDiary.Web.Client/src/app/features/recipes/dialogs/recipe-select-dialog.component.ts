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
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, type Observable, of, switchMap, tap } from 'rxjs';

import type { FormGroupControls } from '../../../shared/lib/common.data';
import { PagedData } from '../../../shared/lib/paged-data.data';
import { RecipeService } from '../api/recipe.service';
import { RecipeManageComponent } from '../components/manage/recipe-manage.component';
import { resolveRecipeImageUrl } from '../lib/recipe-image.util';
import type { Recipe, RecipeFilters } from '../models/recipe.data';

interface RecipeSelectItemViewModel {
    recipe: Recipe;
    imageUrl: string | undefined;
}

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
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconComponent,
        FdUiInputComponent,
    ],
})
export class RecipeSelectDialogComponent {
    private readonly recipeService = inject(RecipeService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject(FdUiDialogRef<RecipeSelectDialogComponent, Recipe | null>, {
        optional: true,
    });
    private readonly fdDialogService = inject(FdUiDialogService);

    public readonly embedded = input<boolean>(false);
    public readonly recipeSelected = output<Recipe>();
    public readonly createRecipeRequested = output<void>();
    public readonly searchValue = signal<string | null>(null);
    public readonly onlyMineFilter = signal(false);
    public readonly searchSuffixIcon = computed(() => (this.searchValue() ? 'close' : undefined));
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
    protected readonly fallbackRecipeImage = 'assets/images/stubs/receipt.png';

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.loadRecipes(1).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                tap(value => {
                    this.searchValue.set(value);
                }),
                debounceTime(300),
                switchMap(() => this.loadRecipes(1)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                distinctUntilChanged(),
                tap(value => {
                    this.onlyMineFilter.set(value);
                }),
                switchMap(() => this.loadRecipes(1)),
            )
            .subscribe();
    }

    public loadRecipes(page: number): Observable<void> {
        this.recipeData.setLoading(true);
        const includePublic = !this.searchForm.controls.onlyMine.value;
        const filters: RecipeFilters = {
            search: this.searchForm.controls.search.value ?? undefined,
        };

        return this.recipeService.query(page, 10, filters, includePublic).pipe(
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
        this.loadRecipes(pageIndex + 1).subscribe();
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
        this.fdDialogService
            .open<RecipeManageComponent, null, Recipe | null>(RecipeManageComponent, {
                preset: 'fullscreen',
            })
            .afterClosed()
            .subscribe(recipe => {
                if (!recipe) {
                    return;
                }
                this.handleSelection(recipe);
            });
    }

    private handleSelection(recipe: Recipe): void {
        if (!this.embedded() && this.dialogRef) {
            this.dialogRef.close(recipe);
        } else {
            this.recipeSelected.emit(recipe);
        }
    }

    private scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    public isPrivateVisibility(visibility: Recipe['visibility']): boolean {
        return visibility.toString().toUpperCase() === 'PRIVATE';
    }
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
