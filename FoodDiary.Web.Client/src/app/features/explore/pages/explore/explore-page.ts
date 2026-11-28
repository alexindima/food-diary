import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { debounceTime, distinctUntilChanged, skip } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card';
import { EXPLORE_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { resolveRecipeImageUrl } from '../../../../shared/lib/recipe-image.util';
import type { Recipe } from '../../../../shared/models/recipe.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { RecipeDetailComponent } from '../../../recipes/contracts/recipe-detail';
import { ExploreInteractionsFacade } from '../../lib/explore-interactions.facade';
import type { ExploreRecipe } from '../../models/explore.data';
import { EXPLORE_PAGE_SIZE, type ExploreSort, type ExploreSortAction } from './explore-page-lib/explore-page.constants';
import { EXPLORE_TOUR } from './explore-tour';

type ExploreQuery = { pageIndex: number; search: string; sortBy: ExploreSort };

@Component({
    selector: 'fd-explore-page',
    templateUrl: './explore-page.html',
    styleUrls: ['./explore-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiPaginationComponent,
        FdUiIconComponent,
        FdUiInlineAlertComponent,
        FdUiLoaderComponent,
        PageHeaderComponent,
        PageBodyComponent,
        RecipeCardComponent,
        FdPageContainerDirective,
    ],
})
export class ExplorePageComponent {
    private readonly exploreInteractionsFacade = inject(ExploreInteractionsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly searchDebounceMs = inject(EXPLORE_SEARCH_DEBOUNCE_MS);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly query = signal<ExploreQuery>({ pageIndex: 0, search: '', sortBy: 'newest' });

    protected readonly searchModel = signal({ search: '' });
    protected readonly searchForm = form(this.searchModel);
    private readonly searchValue = computed(() => this.searchModel().search);
    protected readonly sortActions = computed<ExploreSortAction[]>(() => {
        const selectedSort = this.query().sortBy;

        return [
            { value: 'newest', labelKey: 'EXPLORE.SORT_NEWEST', variant: selectedSort === 'newest' ? 'primary' : 'outline' },
            { value: 'popular', labelKey: 'EXPLORE.SORT_POPULAR', variant: selectedSort === 'popular' ? 'primary' : 'outline' },
        ];
    });
    protected readonly pageSize = EXPLORE_PAGE_SIZE;
    protected readonly currentPageIndex = computed(() => this.query().pageIndex);
    private readonly recipes = rxResource({
        params: this.query,
        stream: ({ params: { pageIndex, search, sortBy } }) =>
            this.exploreInteractionsFacade.queryRecipes(pageIndex + 1, this.pageSize, { search, sortBy }),
    });
    protected readonly isLoading = this.recipes.isLoading;
    protected readonly hasLoadError = computed(() => this.recipes.error() !== undefined);
    protected readonly recipePage = computed(() => (this.recipes.hasValue() ? this.recipes.value() : undefined));

    protected readonly resolveImageUrl = resolveRecipeImageUrl;

    public constructor() {
        toObservable(this.searchValue)
            .pipe(skip(1), debounceTime(this.searchDebounceMs), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.requestRecipes(0);
            });
    }

    protected onSortChange(sort: ExploreSort): void {
        this.requestRecipes(0, sort);
    }

    protected onPageChange(pageIndex: number): void {
        this.requestRecipes(pageIndex);
    }

    protected retryLoad(): void {
        this.recipes.reload();
    }

    protected startExploreTour(force = true): void {
        this.tourService.start(this.localizedTour.build(EXPLORE_TOUR), { force });
    }

    protected onRecipeClick(recipe: ExploreRecipe): void {
        this.fdDialogService.open<RecipeDetailComponent, Recipe>(RecipeDetailComponent, {
            preset: 'detail',
            data: recipe,
        });
    }

    private requestRecipes(pageIndex: number, sortBy = this.query().sortBy): void {
        this.query.set({ pageIndex, search: this.searchValue(), sortBy });
    }
}
