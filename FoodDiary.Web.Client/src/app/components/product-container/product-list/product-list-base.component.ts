import { ChangeDetectionStrategy, Component, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ProductService } from '../../../services/product.service';
import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../types/paged-data.data';
import { Product, ProductFilters } from '../../../types/product.data';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, Observable, of, switchMap, tap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { FormGroupControls } from '../../../types/common.data';
import { BarcodeScannerComponent } from '../../shared/barcode-scanner/barcode-scanner.component';
import { BadgeComponent } from '../../shared/badge/badge.component';
import { FdUiEntityCardComponent } from 'fd-ui-kit/entity-card/fd-ui-entity-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { buildProductTypeTranslationKey } from '../../../utils/product-type.utils';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconModule } from 'fd-ui-kit/material';

@Component({
    selector: 'fd-product-list-base',
    standalone: true,
    templateUrl: './product-list-base.component.html',
    styleUrls: ['./product-list-base.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        BadgeComponent,
        FdUiEntityCardComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconModule,
    ],
})
export class ProductListBaseComponent implements OnInit {
    protected readonly productService = inject(ProductService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly pageSize = 10;
    protected readonly fdDialogService = inject(FdUiDialogService);

    @ViewChild('container') private container!: ElementRef<HTMLElement>;

    public searchForm: FormGroup<ProductSearchFormGroup>;
    public productData: PagedData<Product> = new PagedData<Product>();
    public currentPageIndex = 0;

    public constructor() {
        this.searchForm = new FormGroup<ProductSearchFormGroup>({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        });
    }

    protected isPrivateVisibility(visibility: Product['visibility']): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }

    public ngOnInit(): void {
        this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                debounceTime(300),
                switchMap(value => this.loadProducts(1, this.pageSize, value)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                distinctUntilChanged(),
                switchMap(() => this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value)),
            )
            .subscribe();
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();

        this.currentPageIndex = pageIndex;
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value).subscribe();
    }

    public async onAddProductClick(): Promise<void> {
        await this.navigationService.navigateToProductAdd();
    }

    public openBarcodeScanner(): void {
        this.fdDialogService
            .open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, {
                size: 'lg',
            })
            .afterClosed()
            .subscribe(barcode => {
                if (barcode) {
                    this.searchForm.controls.search.setValue(barcode);
                }
            });
    }

    protected loadProducts(page: number, limit: number, search: string | null): Observable<void> {
        this.productData.setLoading(true);
        const filters = new ProductFilters(search);
        const includePublic = !this.searchForm.controls.onlyMine.value;
        return this.productService.query(page, limit, filters, includePublic).pipe(
            tap(pageData => {
                this.productData.setData(pageData);
                this.currentPageIndex = pageData.page - 1;
            }),
            map(() => void 0),
            catchError((error: HttpErrorResponse) => {
                console.error('Error loading products:', error);
                this.productData.clearData();
                return of(void 0);
            }),
            finalize(() => this.productData.setLoading(false)),
        );
    }

    protected scrollToTop(): void {
        this.container.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected onProductClick(_product: Product): void {}

    protected getProductTypeTranslationKey(product: Product): string {
        return buildProductTypeTranslationKey(product.productType ?? product.category ?? null);
    }
}

interface ProductSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type ProductSearchFormGroup = FormGroupControls<ProductSearchFormValues>;
