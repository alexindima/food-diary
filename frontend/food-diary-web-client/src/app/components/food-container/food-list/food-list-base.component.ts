import { ChangeDetectionStrategy, Component, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { TuiPagination } from '@taiga-ui/kit';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
    TuiButton,
    tuiDialog, TuiIcon,
    TuiLoader,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FoodService } from '../../../services/food.service';
import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../types/paged-data.data';
import { Food, FoodFilters } from '../../../types/food.data';
import { catchError, debounceTime, map, Observable, of, switchMap } from 'rxjs';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { FormGroupControls } from '../../../types/common.data';
import { BarcodeScannerComponent } from '../../shared/barcode-scanner/barcode-scanner.component';

@Component({
    selector: 'app-food-list-base',
    templateUrl: './food-list-base.component.html',
    styleUrls: ['./food-list-base.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TuiPagination,
        ReactiveFormsModule,
        TuiLoader,
        TuiSearchComponent,
        TuiTextfieldControllerModule,
        TuiTextfieldComponent,
        TuiTextfieldDirective,
        TuiButton,
        TranslatePipe,
        TuiIcon,
    ]
})
export class FoodListBaseComponent implements OnInit {
    private readonly translateService = inject(TranslateService);
    protected readonly foodService = inject(FoodService);
    protected readonly navigationService = inject(NavigationService);

    @ViewChild('container') private container!: ElementRef<HTMLElement>;

    private readonly barcodeDialog = tuiDialog(BarcodeScannerComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    public searchForm: FormGroup<FoodSearchFormGroup>;
    public foodData: PagedData<Food> = new PagedData<Food>();
    public currentPageIndex = 0;

    public constructor() {
        this.searchForm = new FormGroup<FoodSearchFormGroup>({
            search: new FormControl<string | null>(null),
        });
    }

    public ngOnInit(): void {
        this.loadFoods(1, 10, this.searchForm.controls.search.value).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                debounceTime(300),
                switchMap(value => this.loadFoods(1, 10, value)),
            )
            .subscribe();
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();

        this.currentPageIndex = pageIndex;
        this.loadFoods(this.currentPageIndex + 1, 10, this.searchForm.controls.search.value).subscribe();
    }

    public getTitle(): string {
        const searchValue = this.searchForm.controls.search.value;
        return searchValue
            ? `${this.translateService.instant('FOOD_LIST.TITLE')} (${this.translateService.instant('FOOD_LIST.SEARCH_TITLE')} ${this.searchForm.get('search')?.value})`
            : this.translateService.instant('FOOD_LIST.TITLE');
    }

    public async onAddFoodClick(): Promise<void> {
        await this.navigationService.navigateToFoodAdd();
    }

    public openBarcodeScanner(): void {
        this.barcodeDialog(null).subscribe({
            next: (barcode) => {
                this.searchForm.controls.search.setValue(barcode);
            },
        });
    }

    protected loadFoods(page: number, limit: number, search: string | null): Observable<void> {
        this.foodData.setLoading(true);
        const filters = new FoodFilters(search);
        return this.foodService.query(page, limit, filters).pipe(
            map(response => {
                if (response.status === 'success' && response.data) {
                    this.foodData.setData(response.data);
                    this.currentPageIndex = page - 1;
                } else {
                    this.foodData.clearData();
                }
                this.foodData.setLoading(false);
            }),
            catchError(() => {
                this.foodData.clearData();
                this.foodData.setLoading(false);
                return of();
            }),
        );
    }

    protected scrollToTop(): void {
        this.container.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected onFoodClick(_food: Food): void {}
}

interface FoodSearchFormValues {
    search: string | null;
}

type FoodSearchFormGroup = FormGroupControls<FoodSearchFormValues>;
