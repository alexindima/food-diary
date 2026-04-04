import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDateRangeInputComponent, FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { Observable, catchError, debounceTime, distinctUntilChanged, map, of, startWith, switchMap } from 'rxjs';

import { MealService } from '../../api/meal.service';
import { MealDetailActionResult, MealDetailComponent } from '../../components/detail/meal-detail.component';
import { Meal, MealFilters } from '../../models/meal.data';
import { MealCardComponent } from '../../../../components/shared/meal-card/meal-card.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../../pipes/localized-date.pipe';
import { NavigationService } from '../../../../services/navigation.service';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { PagedData } from '../../../../shared/lib/paged-data.data';

@Component({
    selector: 'fd-meal-list',
    templateUrl: './meal-list.component.html',
    styleUrls: ['./meal-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDateRangeInputComponent,
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        MealCardComponent,
        LocalizedDatePipe,
    ],
})
export class MealListComponent implements OnInit {
    private readonly mealService = inject(MealService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly breakpointObserver = inject(BreakpointObserver);

    public searchForm: FormGroup<SearchFormGroup>;
    public consumptionData: PagedData<Meal> = new PagedData<Meal>();
    public currentPageIndex = 0;
    public readonly groupedConsumptions = computed(() => this.groupByDate(this.consumptionData.items()));
    public readonly errorKey = signal<string | null>(null);
    public readonly isMobileView = signal<boolean>(window.matchMedia('(max-width: 768px)').matches);
    private readonly isMobileDateFilterOpen = signal(false);
    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.searchForm = new FormGroup<SearchFormGroup>({
            dateRange: new FormControl<FdUiDateRangeValue | null>(null),
        });
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
                    this.isMobileDateFilterOpen.set(false);
                }
            });

        this.searchForm.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                debounceTime(300),
                startWith(this.searchForm.value),
                switchMap(() => this.loadConsumptions(1)),
            )
            .subscribe();
    }

    public loadConsumptions(page: number): Observable<void> {
        this.consumptionData.setLoading(true);
        const dateRange = this.searchForm.controls.dateRange.value;

        const filters: MealFilters = {
            dateFrom: this.toIsoDate(dateRange?.start ?? null),
            dateTo: this.toIsoDate(dateRange?.end ?? null),
        };

        return this.mealService.query(page, 10, filters).pipe(
            map(pageData => {
                this.consumptionData.setData(pageData);
                this.currentPageIndex = pageData.page - 1;
                this.consumptionData.setLoading(false);
                this.errorKey.set(null);
            }),
            catchError(() => {
                this.consumptionData.clearData();
                this.consumptionData.setLoading(false);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of();
            }),
        );
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadConsumptions(pageIndex + 1).subscribe();
    }

    public async openMealDetails(consumption: Meal): Promise<void> {
        this.fdDialogService
            .open<MealDetailComponent, Meal, MealDetailActionResult>(MealDetailComponent, {
                size: 'lg',
                data: consumption,
            })
            .afterClosed()
            .subscribe(data => {
                if (!data) {
                    return;
                }

                if (data.action === 'Edit') {
                    this.navigationService.navigateToConsumptionEdit(data.id);
                } else if (data.action === 'Repeat') {
                    const today = new Date().toISOString().slice(0, 10);
                    this.mealService.repeat(data.id, today).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.loadConsumptions(this.currentPageIndex + 1).subscribe();
                        },
                    });
                } else if (data.action === 'Delete') {
                    this.mealService.deleteById(data.id).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.loadConsumptions(this.currentPageIndex + 1).subscribe();
                        },
                    });
                }
            });
    }

    public async goToMealAdd(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public toggleMobileDateFilter(): void {
        this.isMobileDateFilterOpen.update(value => !value);
    }

    public get hasDateFilter(): boolean {
        const dateRange = this.searchForm.controls.dateRange.value;
        return !!dateRange?.start || !!dateRange?.end;
    }

    public get isMobileDateFilterVisible(): boolean {
        return this.isMobileDateFilterOpen() || this.hasDateFilter;
    }

    public get isEmptyState(): boolean {
        return this.consumptionData.items().length === 0 && !this.hasDateFilter;
    }

    public get isNoResultsState(): boolean {
        return this.consumptionData.items().length === 0 && this.hasDateFilter;
    }

    protected scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    private toIsoDate(date: Date | null | undefined): string | undefined {
        if (!date) {
            return undefined;
        }

        const normalized = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
        return normalized.toISOString();
    }

    private groupByDate(items: Meal[]): { date: Date; items: Meal[] }[] {
        const buckets = new Map<string, { date: Date; items: Meal[] }>();

        for (const item of items) {
            const date = new Date(item.date);
            const key = date.toISOString().slice(0, 10);
            if (!buckets.has(key)) {
                buckets.set(key, { date, items: [] });
            }
            buckets.get(key)!.items.push(item);
        }

        return Array.from(buckets.values()).sort((a, b) => b.date.getTime() - a.date.getTime());
    }
}

interface SearchFormValues {
    dateRange: FdUiDateRangeValue | null;
}

type SearchFormGroup = FormGroupControls<SearchFormValues>;
