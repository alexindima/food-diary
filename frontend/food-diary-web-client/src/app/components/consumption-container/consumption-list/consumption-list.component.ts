import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { catchError, debounceTime, map, Observable, of, startWith, switchMap } from 'rxjs';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiButton, tuiDialog, TuiLoader } from '@taiga-ui/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Consumption, ConsumptionFilters } from '../../../types/consumption.data';
import { ConsumptionService } from '../../../services/consumption.service';
import { TuiInputDateRangeModule, TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { DatePipe, DecimalPipe } from '@angular/common';
import { PagedData } from '../../../types/paged-data.data';
import { TuiDayRange } from '@taiga-ui/cdk';
import { NavigationService } from '../../../services/navigation.service';
import { ConsumptionDetailComponent } from '../consumption-detail/consumption-detail.component';
import { FormGroupControls } from '../../../types/common.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CardComponent } from '../../shared/card/card.component';

@Component({
    selector: 'fd-consumption-list',
    templateUrl: './consumption-list.component.html',
    styleUrls: ['./consumption-list.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TuiPagination,
        ReactiveFormsModule,
        TuiLoader,
        TuiButton,
        TranslatePipe,
        TuiInputDateRangeModule,
        TuiTextfieldControllerModule,
        DatePipe,
        DecimalPipe,
        CardComponent,
    ]
})
export class ConsumptionListComponent implements OnInit {
    private readonly consumptionService = inject(ConsumptionService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);

    public searchForm: FormGroup<SearchFormGroup>;
    public consumptionData: PagedData<Consumption> = new PagedData<Consumption>();
    public currentPageIndex = 0;

    @ViewChild('container') private container!: ElementRef<HTMLElement>;

    public constructor() {
        this.searchForm = new FormGroup<SearchFormGroup>({
            dateRange: new FormControl<TuiDayRange | null>(null),
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

    private readonly dialog = tuiDialog(ConsumptionDetailComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    public loadConsumptions(page: number): Observable<void> {
        this.consumptionData.setLoading(true);
        const dateRange = this.searchForm.controls.dateRange.value as TuiDayRange | null;

        const filters: ConsumptionFilters = {
            dateFrom: dateRange?.from?.toUtcNativeDate().toISOString(),
            dateTo: dateRange?.to?.toUtcNativeDate().toISOString(),
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
        this.dialog(consumption).subscribe({
            next: data => {
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
            },
        });
    }

    public async goToConsumptionAdd(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    protected scrollToTop(): void {
        this.container.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

interface SearchFormValues {
    dateRange: TuiDayRange | null;
}

type SearchFormGroup = FormGroupControls<SearchFormValues>;
