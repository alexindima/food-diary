import { BreakpointObserver } from '@angular/cdk/layout';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { resolveRecipeImageUrl } from '../../lib/recipe-image.util';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card.component';
import { RecipeDetailActionResult, RecipeDetailComponent } from '../../components/detail/recipe-detail.component';
import {
    RecipeListFiltersDialogComponent,
    RecipeListFiltersDialogResult,
} from '../../components/list/recipe-list-filters-dialog.component';
import { Recipe, RecipeVisibility } from '../../models/recipe.data';
import { RecipeListFacade } from '../../lib/recipe-list.facade';

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
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        RecipeCardComponent,
    ],
    providers: [RecipeListFacade],
})
export class RecipeListComponent implements OnInit {
    private readonly translateService = inject(TranslateService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly breakpointObserver = inject(BreakpointObserver);
    private readonly destroyRef = inject(DestroyRef);
    private readonly recipeListFacade = inject(RecipeListFacade);

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public readonly pageSize = this.recipeListFacade.pageSize;
    public recipeData = this.recipeListFacade.recipeData;
    public currentPageIndex = this.recipeListFacade.currentPageIndex;
    public recentRecipes = this.recipeListFacade.recentRecipes;
    public readonly errorKey = this.recipeListFacade.errorKey;
    public readonly isMobileView = signal<boolean>(window.matchMedia('(max-width: 768px)').matches);
    private readonly isMobileSearchOpen = signal(false);
    public searchForm: FormGroup<RecipeSearchFormGroup>;
    public readonly isDeleting = this.recipeListFacade.isDeleting;
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

        this.recipeListFacade
            .loadRecipes(1, this.pageSize, this.searchForm.controls.search.value, this.searchForm.controls.onlyMine.value)
            .subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                debounceTime(300),
                switchMap(value => this.recipeListFacade.loadRecipes(1, this.pageSize, value, this.searchForm.controls.onlyMine.value)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                distinctUntilChanged(),
                switchMap(() =>
                    this.recipeListFacade.loadRecipes(
                        1,
                        this.pageSize,
                        this.searchForm.controls.search.value,
                        this.searchForm.controls.onlyMine.value,
                    ),
                ),
            )
            .subscribe();
    }

    public retryLoad(): void {
        this.recipeListFacade
            .loadRecipes(1, this.pageSize, this.searchForm.controls.search.value, this.searchForm.controls.onlyMine.value)
            .subscribe();
    }

    public async onAddRecipeClick(): Promise<void> {
        await this.recipeListFacade.navigateToAddRecipe();
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
                void this.recipeListFacade.handleDetailAction(
                    result,
                    recipe,
                    this.searchForm.controls.search.value,
                    this.searchForm.controls.onlyMine.value,
                );
            });
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex.set(pageIndex);
        this.recipeListFacade
            .loadRecipes(
                this.currentPageIndex() + 1,
                this.pageSize,
                this.searchForm.controls.search.value,
                this.searchForm.controls.onlyMine.value,
            )
            .subscribe();
    }

    public async onEditRecipe(recipe: Recipe): Promise<void> {
        await this.recipeListFacade.navigateToEditRecipe(recipe.id);
    }

    public onDeleteRecipe(recipe: Recipe): void {
        this.recipeListFacade
            .deleteRecipe(recipe, this.searchForm.controls.search.value, this.searchForm.controls.onlyMine.value)
            .subscribe();
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
        this.recipeListFacade.addToMeal(recipe);
    }

    public get showRecentSection(): boolean {
        return this.recipeListFacade.showRecentSection();
    }

    public get recentRecipeItems(): Recipe[] {
        return this.recentRecipes();
    }

    public get allRecipesSectionItems(): Recipe[] {
        return this.recipeListFacade.allRecipesSectionItems();
    }

    public get hasVisibleRecipes(): boolean {
        return this.recipeListFacade.hasVisibleRecipes();
    }

    public get hasActiveFilters(): boolean {
        return this.recipeListFacade.hasActiveFilters(this.searchForm.controls.onlyMine.value);
    }

    public get isEmptyState(): boolean {
        return !this.hasVisibleRecipes && !this.recipeListFacade.hasSearch(this.searchForm.controls.search.value) && !this.hasActiveFilters;
    }

    public get isNoResultsState(): boolean {
        return !this.hasVisibleRecipes && !this.isEmptyState;
    }

    public get allRecipesSectionLabelKey(): string {
        return this.recipeListFacade.allRecipesSectionLabelKey();
    }

    public get isMobileSearchVisible(): boolean {
        return this.isMobileSearchOpen() || this.recipeListFacade.hasSearch(this.searchForm.controls.search.value);
    }

    public get pageIndex(): number {
        return this.currentPageIndex();
    }

    public isPrivateVisibility(visibility: RecipeVisibility | string | null | undefined): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }

    private scrollToTop(): void {
        this.container()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
