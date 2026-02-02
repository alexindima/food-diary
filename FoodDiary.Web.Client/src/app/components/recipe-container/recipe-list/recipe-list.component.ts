import { ChangeDetectionStrategy, Component, ElementRef, inject, OnInit, viewChild } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup } from '@angular/forms';
import { Recipe, RecipeFilters, RecipeVisibility } from '../../../types/recipe.data';
import { RecipeService } from '../../../services/recipe.service';
import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../types/paged-data.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { catchError, debounceTime, finalize, map, Observable, of, switchMap, tap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { FormGroupControls } from '../../../types/common.data';
import { RecipeDetailComponent, RecipeDetailActionResult } from '../recipe-detail/recipe-detail.component';
import { FdUiPlainInputComponent } from 'fd-ui-kit/plain-input/fd-ui-plain-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { PageBodyComponent } from '../../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { resolveRecipeImageUrl } from '../../../utils/recipe-stub.utils';
import { RecipeCardComponent } from '../../shared/recipe-card/recipe-card.component';
import { QuickConsumptionService } from '../../../services/quick-consumption.service';

@Component({
    selector: 'fd-recipe-list',
    templateUrl: './recipe-list.component.html',
    styleUrls: ['./recipe-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiPlainInputComponent,
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

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public readonly pageSize = 10;
    public recipeData: PagedData<Recipe> = new PagedData<Recipe>();
    public currentPageIndex = 0;
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
        this.loadRecipes(1, this.pageSize, this.searchForm.controls.search.value).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                debounceTime(300),
                switchMap(value => this.loadRecipes(1, this.pageSize, value)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
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
                    this.toastService.open(
                        this.translateService.instant('RECIPE_LIST.DELETE_ERROR'),
                        { appearance: 'negative' },
                    );
                },
            });
    }

    public trackByRecipeId(_index: number, recipe: Recipe): string {
        return recipe.id;
    }

    public getVisibilityTranslation(visibility: RecipeVisibility): string {
        return this.translateService.instant(`RECIPE_VISIBILITY.${visibility}`);
    }

    private loadRecipes(page: number, limit: number, search: string | null): Observable<void> {
        this.recipeData.setLoading(true);
        const filters: RecipeFilters = { search };
        const includePublic = !this.searchForm.controls.onlyMine.value;

        return this.recipeService.query(page, limit, filters, includePublic).pipe(
            tap(pageData => {
                this.recipeData.setData(pageData);
                this.currentPageIndex = pageData.page - 1;
            }),
            map(() => void 0),
            catchError((error: HttpErrorResponse) => {
                console.error('Error loading recipes:', error);
                this.recipeData.clearData();
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

    public isPrivateVisibility(visibility: RecipeVisibility | string | null | undefined): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }

    public toggleOnlyMine(): void {
        const control = this.searchForm.controls.onlyMine;
        control.setValue(!control.value);
    }

    public clearSearch(): void {
        this.searchForm.controls.search.setValue('');
    }

    public onAddToMeal(recipe: Recipe): void {
        this.quickConsumptionService.addRecipe(recipe);
    }
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
