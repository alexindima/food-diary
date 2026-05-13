import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';

@Component({
    selector: 'fd-product-detail-actions',
    imports: [TranslatePipe, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './product-detail-actions.component.html',
    styleUrl: '../product-detail/product-detail.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDetailActionsComponent {
    public readonly canModify = input.required<boolean>();
    public readonly warningMessage = input.required<string | null>();
    public readonly isDuplicateInProgress = input.required<boolean>();

    public readonly edit = output();
    public readonly delete = output();
    public readonly duplicate = output();
}
