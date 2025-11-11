import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import {
    TuiButton,
    TuiDialogContext,
    TuiIcon,
    TuiLoader,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { TranslatePipe } from '@ngx-translate/core';
import { Recipe, RecipeFilters } from '../../../types/recipe.data';
import { RecipeService } from '../../../services/recipe.service';
import { injectContext } from '@taiga-ui/polymorpheus';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, Observable, of, switchMap, tap } from 'rxjs';
import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../types/paged-data.data';
import { FormGroupControls } from '../../../types/common.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CardComponent } from '../../shared/card/card.component';
import { BadgeComponent } from '../../shared/badge/badge.component';

@Component({
    selector: 'fd-recipe-select-dialog',
    standalone: true,
    templateUrl: './recipe-select-dialog.component.html',
    styleUrls: ['./recipe-select-dialog.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        DecimalPipe,
        TranslatePipe,
        TuiButton,
        TuiLoader,
        TuiPagination,
        TuiSearchComponent,
        TuiTextfieldComponent,
        TuiTextfieldDirective,
        TuiTextfieldControllerModule,
        TuiIcon,
        CardComponent,
        BadgeComponent,
    ],
})
export class RecipeSelectDialogComponent implements OnInit {
    private readonly recipeService = inject(RecipeService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly context = injectContext<TuiDialogContext<Recipe | null, null>>();

    public searchForm: FormGroup<RecipeSearchFormGroup>;
    public recipeData: PagedData<Recipe> = new PagedData<Recipe>();
    public currentPageIndex = 0;

    @ViewChild('container') private container!: ElementRef<HTMLElement>;

    public constructor() {
        this.searchForm = new FormGroup<RecipeSearchFormGroup>({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        });
    }

    public ngOnInit(): void {
        this.loadRecipes(1).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                debounceTime(300),
                switchMap(() => this.loadRecipes(1)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                distinctUntilChanged(),
                switchMap(() => this.loadRecipes(1)),
            )
            .subscribe();
    }

    public loadRecipes(page: number): Observable<void> {
        this.recipeData.setLoading(true);
        const includePublic = !this.searchForm.controls.onlyMine.value;
        const filters: RecipeFilters = {
            search: this.searchForm.controls.search.value || undefined,
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
            finalize(() => this.recipeData.setLoading(false)),
        );
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadRecipes(pageIndex + 1).subscribe();
    }

    public onRecipeClick(recipe: Recipe): void {
        this.context.completeWith(recipe);
    }

    public async onCreateRecipeClick(): Promise<void> {
        this.context.completeWith(null);
        await this.navigationService.navigateToRecipeAdd();
    }

    private scrollToTop(): void {
        this.container.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
