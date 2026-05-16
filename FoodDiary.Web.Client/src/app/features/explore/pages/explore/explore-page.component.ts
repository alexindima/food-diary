import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card.component';
import { EXPLORE_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { RecipeDetailComponent } from '../../../recipes/components/detail/recipe-detail/recipe-detail.component';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import type { Recipe } from '../../../recipes/models/recipe.data';
import { ExploreService } from '../../api/explore.service';
import type { ExploreFilters, ExploreRecipe } from '../../models/explore.data';
import { EXPLORE_PAGE_SIZE, type ExploreSort, type ExploreSortAction } from './explore-page-lib/explore-page.constants';

@Component({
    selector: 'fd-explore-page',
    templateUrl: './explore-page.component.html',
    styleUrls: ['./explore-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiPaginationComponent,
        FdUiIconComponent,
        FdUiLoaderComponent,
        PageHeaderComponent,
        PageBodyComponent,
        RecipeCardComponent,
        FdPageContainerDirective,
    ],
})
export class ExplorePageComponent {
    private readonly exploreService = inject(ExploreService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly searchDebounceMs = inject(EXPLORE_SEARCH_DEBOUNCE_MS);

    public readonly searchControl = new FormControl('');
    public readonly sortBy = signal<ExploreSort>('newest');
    public readonly sortActions = computed<ExploreSortAction[]>(() => {
        const selectedSort = this.sortBy();

        return [
            { value: 'newest', labelKey: 'EXPLORE.SORT_NEWEST', variant: selectedSort === 'newest' ? 'primary' : 'outline' },
            { value: 'popular', labelKey: 'EXPLORE.SORT_POPULAR', variant: selectedSort === 'popular' ? 'primary' : 'outline' },
        ];
    });
    public readonly recipeData = new PagedData<ExploreRecipe>();
    public readonly currentPageIndex = signal(0);
    public readonly pageSize = EXPLORE_PAGE_SIZE;

    public readonly resolveImageUrl = resolveRecipeImageUrl;

    public constructor() {
        this.loadRecipes();

        this.searchControl.valueChanges
            .pipe(debounceTime(this.searchDebounceMs), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.currentPageIndex.set(0);
                this.loadRecipes();
            });
    }

    public onSortChange(sort: ExploreSort): void {
        this.sortBy.set(sort);
        this.currentPageIndex.set(0);
        this.loadRecipes();
    }

    public onPageChange(pageIndex: number): void {
        this.currentPageIndex.set(pageIndex);
        this.loadRecipes();
    }

    public onRecipeClick(recipe: ExploreRecipe): void {
        this.fdDialogService.open<RecipeDetailComponent, Recipe>(RecipeDetailComponent, {
            preset: 'detail',
            data: recipe,
        });
    }

    private loadRecipes(): void {
        const filters: ExploreFilters = {
            search: this.searchControl.value ?? undefined,
            sortBy: this.sortBy(),
        };

        this.recipeData.setLoading(true);
        this.exploreService
            .query(this.currentPageIndex() + 1, this.pageSize, filters)
            .pipe(
                finalize(() => {
                    this.recipeData.setLoading(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(data => {
                this.recipeData.setData(data);
            });
    }
}
