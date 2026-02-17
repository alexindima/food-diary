import { BreakpointObserver } from '@angular/cdk/layout';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, Observable, of, switchMap, tap } from 'rxjs';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../services/navigation.service';
import { QuickConsumptionService } from '../../../services/quick-consumption.service';
import { RecipeService } from '../../../services/recipe.service';
import { FormGroupControls } from '../../../types/common.data';
import { PagedData } from '../../../types/paged-data.data';
import { Recipe, RecipeFilters, RecipeVisibility } from '../../../types/recipe.data';
import { resolveRecipeImageUrl } from '../../../utils/recipe-stub.utils';
import { PageBodyComponent } from '../../shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { RecipeCardComponent } from '../../shared/recipe-card/recipe-card.component';
import { RecipeDetailActionResult, RecipeDetailComponent } from '../recipe-detail/recipe-detail.component';
import {
    RecipeListFiltersDialogComponent,
    RecipeListFiltersDialogResult,
} from './recipe-list-filters-dialog/recipe-list-filters-dialog.component';

@Component({
    selector: 'fd-recipe-list',
    templateUrl: './recipe-list.component.html',
    styleUrls: ['./recipe-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        RecipeCardComponent,
    ],
})
export class RecipeListComponent implements OnInit {
    private readonly recipeService = inject(RecipeService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly toastService = inject(FdUiToastService);
    private readonly quickConsumptionService = inject(QuickConsumptionService);
    private readonly breakpointObserver = inject(BreakpointObserver);
    private readonly destroyRef = inject(DestroyRef);

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public readonly pageSize = 10;
    public recipeData: PagedData<Recipe> = new PagedData<Recipe>();
    public currentPageIndex = 0;
    public recentRecipes: Recipe[] = [];
    public readonly isMobileView = signal<boolean>(window.matchMedia('(max-width: 768px)').matches);
    private readonly isMobileSearchOpen = signal(false);
    public searchForm: FormGroup<RecipeSearchFormGroup>;
    public isDeleting = false;
    protected readonly fallbackRecipeImage = 'assets/images/stubs/receipt.png';

    public constructor() {
        this.searchForm = new FormGroup<RecipeSearchFormGroup>({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        });
    }

    public resolveImage(recipe: Recipe): string | undefined {
        return resolveRecipeImageUrl(recipe.imageUrl ?? undefined);
    }

    public ngOnInit(): void {
        this.breakpointObserver
            .observe('(max-width: 768px)')
            .pipe(
                map(result => result.matches),
                distinctUntilChanged(),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(isMobile => {
                this.isMobileView.set(isMobile);
                if (!isMobile) {
                    this.isMobileSearchOpen.set(false);
                }
            });

        this.loadRecipes(1, this.pageSize, this.searchForm.controls.search.value).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                debounceTime(300),
                switchMap(value => this.loadRecipes(1, this.pageSize, value)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                distinctUntilChanged(),
                switchMap(() => this.loadRecipes(1, this.pageSize, this.searchForm.controls.search.value)),
            )
            .subscribe();
    }

    public async onAddRecipeClick(): Promise<void> {
        await this.navigationService.navigateToRecipeAdd();
    }

    public onRecipeClick(recipe: Recipe): void {
        this.fdDialogService
            .open<RecipeDetailComponent, Recipe, RecipeDetailActionResult>(RecipeDetailComponent, {
                size: 'lg',
                data: recipe,
            })
            .afterClosed()
            .subscribe(result => {
                if (!result) {
                    return;
                }
                this.handleDialogResult(result, recipe);
            });
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadRecipes(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value).subscribe();
    }

    public async onEditRecipe(recipe: Recipe): Promise<void> {
        await this.navigationService.navigateToRecipeEdit(recipe.id);
    }

    public onDeleteRecipe(recipe: Recipe): void {
        if (!recipe.isOwnedByCurrentUser || recipe.usageCount > 0 || this.isDeleting) {
            return;
        }

        this.isDeleting = true;
        this.recipeData.setLoading(true);
        this.recipeService
            .deleteById(recipe.id)
            .pipe(
                finalize(() => {
                    this.isDeleting = false;
                }),
            )
            .subscribe({
                next: () => {
                    this.loadRecipes(1, this.pageSize, this.searchForm.controls.search.value)
                        .pipe(finalize(() => this.recipeData.setLoading(false)))
                        .subscribe();
                },
                error: error => {
                    console.error('Delete recipe error', error);
                    this.recipeData.setLoading(false);
                    this.toastService.open(this.translateService.instant('RECIPE_LIST.DELETE_ERROR'), { appearance: 'negative' });
                },
            });
    }

    public trackByRecipeId(_index: number, recipe: Recipe): string {
        return recipe.id;
    }

    public getVisibilityTranslation(visibility: RecipeVisibility): string {
        return this.translateService.instant(`RECIPE_VISIBILITY.${visibility}`);
    }

    public toggleMobileSearch(): void {
        this.isMobileSearchOpen.update(value => !value);
    }

    public openFilters(): void {
        const currentOnlyMine = this.searchForm.controls.onlyMine.value;
        this.fdDialogService
            .open<RecipeListFiltersDialogComponent, { onlyMine: boolean }, RecipeListFiltersDialogResult | null>(
                RecipeListFiltersDialogComponent,
                {
                    size: 'sm',
                    data: { onlyMine: currentOnlyMine },
                },
            )
            .afterClosed()
            .subscribe(result => {
                if (!result || result.onlyMine === currentOnlyMine) {
                    return;
                }

                this.searchForm.controls.onlyMine.setValue(result.onlyMine);
            });
    }

    public clearSearch(): void {
        this.searchForm.controls.search.setValue('');
    }

    public onAddToMeal(recipe: Recipe): void {
        this.quickConsumptionService.addRecipe(recipe);
    }

    public get showRecentSection(): boolean {
        return !this.hasSearchValue(this.searchForm.controls.search.value) && this.recentRecipes.length > 0;
    }

    public get allRecipesSectionItems(): Recipe[] {
        const recipes = this.recipeData.items();
        if (recipes.length === 0) {
            return [];
        }

        if (!this.showRecentSection) {
            return recipes;
        }

        const recentIds = new Set(this.recentRecipes.map(recipe => recipe.id));
        return recipes.filter(recipe => !recentIds.has(recipe.id));
    }

    public get hasVisibleRecipes(): boolean {
        return this.showRecentSection || this.allRecipesSectionItems.length > 0;
    }

    public get hasActiveFilters(): boolean {
        return this.searchForm.controls.onlyMine.value;
    }

    public get allRecipesSectionLabelKey(): string {
        return this.hasSearchValue(this.searchForm.controls.search.value)
            ? 'RECIPE_LIST.SEARCH_RESULTS'
            : 'RECIPE_LIST.ALL_RECIPES';
    }

    public get isMobileSearchVisible(): boolean {
        return this.isMobileSearchOpen() || this.hasSearchValue(this.searchForm.controls.search.value);
    }

    public isPrivateVisibility(visibility: RecipeVisibility | string | null | undefined): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }

    private loadRecipes(page: number, limit: number, search: string | null): Observable<void> {
        this.recipeData.setLoading(true);
        const filters: RecipeFilters = { search };
        const includePublic = !this.searchForm.controls.onlyMine.value;

        return this.recipeService.queryWithRecent(page, limit, filters, includePublic, 10).pipe(
            tap(data => {
                this.recipeData.setData(data.allRecipes);
                this.recentRecipes = data.recentItems;
                this.currentPageIndex = data.allRecipes.page - 1;
            }),
            map(() => void 0),
            catchError((error: HttpErrorResponse) => {
                console.error('Error loading recipes:', error);
                this.recipeData.clearData();
                this.recentRecipes = [];
                return of(void 0);
            }),
            finalize(() => this.recipeData.setLoading(false)),
        );
    }

    private scrollToTop(): void {
        this.container()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    private handleDialogResult(result: RecipeDetailActionResult, recipe: Recipe): void {
        if (result.action === 'Edit') {
            void this.onEditRecipe(recipe);
            return;
        }

        if (result.action === 'Duplicate') {
            void this.navigationService.navigateToRecipeEdit(result.id);
            return;
        }

        if (result.action === 'Delete') {
            this.onDeleteRecipe(recipe);
        }
    }

    private hasSearchValue(value: string | null): boolean {
        return !!value?.trim();
    }
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
