import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Input, Optional, Output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ProductListBaseComponent } from '../product-list-base.component';
import {
    TuiButton,
    tuiDialog,
    TuiDialogContext,
    TuiIcon,
    TuiLoader,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { Product } from '../../../../types/product.data';
import { POLYMORPHEUS_CONTEXT } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { ProductAddDialogComponent } from '../../product-manage/product-add-dialog/product-add-dialog.component';
import { CardComponent } from '../../../shared/card/card.component';
import { BadgeComponent } from '../../../shared/badge/badge.component';

@Component({
    selector: 'fd-product-list-dialog',
    standalone: true,
    templateUrl: '../product-list-base.component.html',
    styleUrls: ['./product-list-dialog.component.less', '../product-list-base.component.less'],
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
        BadgeComponent,
    ]
})
export class ProductListDialogComponent extends ProductListBaseComponent {
    @Input() public embedded: boolean = false;
    @Output() public productSelected = new EventEmitter<Product>();

    public constructor(
        @Optional() @Inject(POLYMORPHEUS_CONTEXT) private readonly context: TuiDialogContext<Product, null> | null,
    ) {
        super();
    }

    private readonly addProductDialog = tuiDialog(ProductAddDialogComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    public override async onAddProductClick(): Promise<void> {
        this.addProductDialog(null).subscribe({
            next: (product) => {
                if (product) {
                    this.handleSelection(product);
                }
            },
        });
    }

    protected override async onProductClick(product: Product): Promise<void> {
        this.handleSelection(product);
    }

    private handleSelection(product: Product): void {
        if (!this.embedded && this.context) {
            this.context.completeWith(product);
        } else {
            this.productSelected.emit(product);
        }
    }
}
