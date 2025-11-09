import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FoodListBaseComponent } from '../food-list-base.component';
import {
    TuiButton,
    tuiDialog,
    TuiIcon,
    TuiLoader,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { Product } from '../../../../types/product.data';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { FoodDetailComponent } from '../../food-detail/food-detail.component';
import { CardComponent } from '../../../shared/card/card.component';

@Component({
    selector: 'fd-food-list-page',
    templateUrl: '../food-list-base.component.html',
    styleUrls: ['./food-list-page.component.less', '../food-list-base.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiButton,
        TuiLoader,
        TuiPagination,
        TuiSearchComponent,
        TuiTextfieldComponent,
        TuiTextfieldControllerModule,
        TuiTextfieldDirective,
        TuiIcon,
        CardComponent,
    ]
})
export class FoodListPageComponent extends FoodListBaseComponent implements OnInit {
    private readonly dialog = tuiDialog(FoodDetailComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    protected override async onProductClick(product: Product): Promise<void> {
        this.dialog(product).subscribe({
            next: data => {
                if (data.action === 'Edit') {
                    this.navigationService.navigateToFoodEdit(data.id);
                } else if (data.action === 'Delete') {
                    if (!product.isOwnedByCurrentUser) {
                        return;
                    }
                    this.productService.deleteById(data.id).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.loadProducts(
                                this.currentPageIndex + 1,
                                this.pageSize,
                                this.searchForm.controls.search.value,
                            ).subscribe();
                        },
                    });
                }
            },
        });
    }
}
