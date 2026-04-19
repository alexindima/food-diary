import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ExploreService } from '../../api/explore.service';
import { ExploreFilters, ExploreRecipe } from '../../models/explore.data';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { RecipeDetailComponent } from '../../../recipes/components/detail/recipe-detail.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { Recipe } from '../../../recipes/models/recipe.data';

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
        FdUiIconModule,
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

    public readonly searchControl = new FormControl('');
    public readonly sortBy = signal<'newest' | 'popular'>('newest');
    public readonly recipeData = new PagedData<ExploreRecipe>();
    public readonly currentPageIndex = signal(0);
    public readonly pageSize = 20;

    public readonly resolveImageUrl = resolveRecipeImageUrl;

    public constructor() {
        this.loadRecipes();

        this.searchControl.valueChanges
            .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.currentPageIndex.set(0);
                this.loadRecipes();
            });
    }

    public onSortChange(sort: 'newest' | 'popular'): void {
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
            size: 'lg',
            data: recipe as Recipe,
            panelClass: 'fd-ui-dialog-panel--detail',
            backdropClass: 'fd-ui-dialog-backdrop--detail',
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
                finalize(() => this.recipeData.setLoading(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(data => this.recipeData.setData(data));
    }
}
