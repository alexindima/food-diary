import { ChangeDetectionStrategy, Component, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup } from '@angular/forms';
import { Recipe, RecipeFilters, RecipeVisibility } from '../../../types/recipe.data';
import { RecipeService } from '../../../services/recipe.service';
import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../types/paged-data.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiButton, TuiIcon, TuiLoader, TuiTextfieldComponent, TuiTextfieldDirective, tuiDialog } from '@taiga-ui/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { CardComponent } from '../../shared/card/card.component';
import { catchError, debounceTime, finalize, map, Observable, of, switchMap, tap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { FormGroupControls } from '../../../types/common.data';
import { TuiAlertService } from '@taiga-ui/core';
import { RecipeDetailComponent, RecipeDetailActionResult } from '../recipe-detail/recipe-detail.component';
import { BadgeComponent } from '../../shared/badge/badge.component';

@Component({
    selector: 'fd-recipe-list',
    templateUrl: './recipe-list.component.html',
    styleUrls: ['./recipe-list.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiLoader,
        TuiPagination,
        TuiSearchComponent,
        TuiTextfieldComponent,
        TuiTextfieldControllerModule,
        TuiTextfieldDirective,
        TuiButton,
        TuiIcon,
        CardComponent,
        BadgeComponent,
    ],
})
export class RecipeListComponent implements OnInit {
    private readonly recipeService = inject(RecipeService);
    private readonly navigationService = inject(NavigationService);
    private readonly alertService = inject(TuiAlertService);
    private readonly translateService = inject(TranslateService);

    @ViewChild('container') private container!: ElementRef<HTMLElement>;

    public readonly pageSize = 10;
    public recipeData: PagedData<Recipe> = new PagedData<Recipe>();
    public currentPageIndex = 0;
    public searchForm: FormGroup<RecipeSearchFormGroup>;
    public isDeleting = false;

    private readonly detailDialog = tuiDialog(RecipeDetailComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    public constructor() {
        this.searchForm = new FormGroup<RecipeSearchFormGroup>({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        });
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
        this.detailDialog(recipe).subscribe({
            next: result => {
                if (!result) {
                    return;
                }
                this.handleDialogResult(result, recipe);
            },
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
                    this.alertService
                        .open(
                            this.translateService.instant('RECIPE_LIST.DELETE_ERROR'),
                            { appearance: 'negative' },
                        )
                        .subscribe();
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
        this.container?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
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

    public getIngredientCount(recipe: Recipe): number {
        if (!recipe?.steps?.length) {
            return 0;
        }

        return recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    }

    public getPrepTime(recipe: Recipe): number | null {
        const prep = recipe.prepTime ?? 0;
        const cook = recipe.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    }
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
