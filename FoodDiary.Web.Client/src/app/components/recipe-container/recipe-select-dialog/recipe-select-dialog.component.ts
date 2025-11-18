import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    ElementRef,
    EventEmitter,
    Input,
    OnInit,
    Output,
    ViewChild,
    inject,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { TranslatePipe } from '@ngx-translate/core';
import { Recipe, RecipeFilters } from '../../../types/recipe.data';
import { RecipeService } from '../../../services/recipe.service';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, Observable, of, switchMap, tap } from 'rxjs';
import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../types/paged-data.data';
import { FormGroupControls } from '../../../types/common.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BadgeComponent } from '../../shared/badge/badge.component';
import { FdUiEntityCardComponent } from 'fd-ui-kit/entity-card/fd-ui-entity-card.component';
import { FdUiEntityCardHeaderDirective } from 'fd-ui-kit/entity-card/fd-ui-entity-card-header.directive';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

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
        BadgeComponent,
        FdUiEntityCardComponent,
        FdUiEntityCardHeaderDirective,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconModule,
        FdUiInputComponent,
    ],
})
export class RecipeSelectDialogComponent implements OnInit {
    private readonly recipeService = inject(RecipeService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject(FdUiDialogRef<RecipeSelectDialogComponent, Recipe | null>, {
        optional: true,
    });

    @Input() public embedded: boolean = false;
    @Output() public recipeSelected = new EventEmitter<Recipe>();
    @Output() public createRecipeRequested = new EventEmitter<void>();

    public readonly searchForm = new FormGroup<RecipeSearchFormGroup>({
        search: new FormControl<string | null>(null),
        onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
    });

    public constructor() {}
    public recipeData: PagedData<Recipe> = new PagedData<Recipe>();
    public currentPageIndex = 0;

    @ViewChild('container') private container!: ElementRef<HTMLElement>;


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
        this.handleSelection(recipe);
    }

    public async onCreateRecipeClick(): Promise<void> {
        if (!this.embedded && this.dialogRef) {
            this.dialogRef.close(null);
        } else {
            this.createRecipeRequested.emit();
        }
        await this.navigationService.navigateToRecipeAdd();
    }

    private handleSelection(recipe: Recipe): void {
        if (!this.embedded && this.dialogRef) {
            this.dialogRef.close(recipe);
        } else {
            this.recipeSelected.emit(recipe);
        }
    }

    private scrollToTop(): void {
        this.container.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    public isPrivateVisibility(visibility: Recipe['visibility']): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
