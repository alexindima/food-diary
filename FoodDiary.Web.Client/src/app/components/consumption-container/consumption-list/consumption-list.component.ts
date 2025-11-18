import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { catchError, debounceTime, map, Observable, of, startWith, switchMap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { Consumption, ConsumptionFilters } from '../../../types/consumption.data';
import { ConsumptionService } from '../../../services/consumption.service';
import { DatePipe, DecimalPipe } from '@angular/common';
import { PagedData } from '../../../types/paged-data.data';
import { NavigationService } from '../../../services/navigation.service';
import {
    ConsumptionDetailComponent,
    ConsumptionDetailActionResult,
} from '../consumption-detail/consumption-detail.component';
import { FormGroupControls } from '../../../types/common.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDateInputComponent, FdUiDateRangeValue } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiEntityCardComponent } from 'fd-ui-kit/entity-card/fd-ui-entity-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiIconModule } from 'fd-ui-kit/material';

@Component({
    selector: 'fd-consumption-list',
    templateUrl: './consumption-list.component.html',
    styleUrls: ['./consumption-list.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        DatePipe,
        DecimalPipe,
        FdUiButtonComponent,
        FdUiDateInputComponent,
        FdUiEntityCardComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconModule,
    ]
})
export class ConsumptionListComponent implements OnInit {
    private readonly consumptionService = inject(ConsumptionService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);

    public searchForm: FormGroup<SearchFormGroup>;
    public consumptionData: PagedData<Consumption> = new PagedData<Consumption>();
    public currentPageIndex = 0;

    @ViewChild('container') private container!: ElementRef<HTMLElement>;

    public constructor() {
        this.searchForm = new FormGroup<SearchFormGroup>({
            dateRange: new FormControl<FdUiDateRangeValue | null>(null),
        });
    }

    public ngOnInit(): void {
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

    protected scrollToTop(): void {
        this.container.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    private toIsoDate(date: Date | null | undefined): string | undefined {
        if (!date) {
            return undefined;
        }

        const normalized = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
        return normalized.toISOString();
    }
}

interface SearchFormValues {
    dateRange: FdUiDateRangeValue | null;
}

type SearchFormGroup = FormGroupControls<SearchFormValues>;
