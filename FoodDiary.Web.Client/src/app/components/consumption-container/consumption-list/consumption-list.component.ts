import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDateRangeInputComponent, FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { catchError, debounceTime, distinctUntilChanged, map, Observable, of, startWith, switchMap } from 'rxjs';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { ConsumptionService } from '../../../services/consumption.service';
import { NavigationService } from '../../../services/navigation.service';
import { FormGroupControls } from '../../../types/common.data';
import { Consumption, ConsumptionFilters } from '../../../types/consumption.data';
import { PagedData } from '../../../types/paged-data.data';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';
import { MealCardComponent } from '../../shared/meal-card/meal-card.component';
import { PageBodyComponent } from '../../shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { ConsumptionDetailActionResult, ConsumptionDetailComponent } from '../consumption-detail/consumption-detail.component';

@Component({
    selector: 'fd-consumption-list',
    templateUrl: './consumption-list.component.html',
    styleUrls: ['./consumption-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDateRangeInputComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        MealCardComponent,
        LocalizedDatePipe,
    ],
})
export class ConsumptionListComponent implements OnInit {
    private readonly consumptionService = inject(ConsumptionService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly breakpointObserver = inject(BreakpointObserver);

    public searchForm: FormGroup<SearchFormGroup>;
    public consumptionData: PagedData<Consumption> = new PagedData<Consumption>();
    public currentPageIndex = 0;
    public readonly groupedConsumptions = computed(() => this.groupByDate(this.consumptionData.items()));
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

        const filters: ConsumptionFilters = {
            dateFrom: this.toIsoDate(dateRange?.start ?? null),
            dateTo: this.toIsoDate(dateRange?.end ?? null),
        };

        return this.consumptionService.query(page, 10, filters).pipe(
            map(pageData => {
                this.consumptionData.setData(pageData);
                this.currentPageIndex = pageData.page - 1;
                this.consumptionData.setLoading(false);
            }),
            catchError(() => {
                this.consumptionData.clearData();
                this.consumptionData.setLoading(false);
                return of();
            }),
        );
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadConsumptions(pageIndex + 1).subscribe();
    }

    public async openConsumptionDetails(consumption: Consumption): Promise<void> {
        this.fdDialogService
            .open<ConsumptionDetailComponent, Consumption, ConsumptionDetailActionResult>(ConsumptionDetailComponent, {
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
                } else if (data.action === 'Delete') {
                    this.consumptionService.deleteById(data.id).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.loadConsumptions(this.currentPageIndex + 1).subscribe();
                        },
                    });
                }
            });
    }

    public async goToConsumptionAdd(): Promise<void> {
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

    private groupByDate(items: Consumption[]): { date: Date; items: Consumption[] }[] {
        const buckets = new Map<string, { date: Date; items: Consumption[] }>();

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
