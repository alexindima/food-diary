import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import type { ShoppingMergeGroup } from '../../lib/shopping-list-consolidation';
import { formatShoppingListItemMeta } from '../../lib/shopping-list-item.mapper';

@Component({
    selector: 'fd-shopping-merge-preview',
    templateUrl: './shopping-merge-preview.html',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiDialogComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingMergePreviewComponent {
    private readonly data = inject<ShoppingMergeGroup[]>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<ShoppingMergePreviewComponent, boolean>);
    private readonly translate = inject(TranslateService);
    protected readonly rows = this.data.map(group => ({
        name: group.after.name,
        before: group.before
            .map(item =>
                formatShoppingListItemMeta(
                    { ...item, category: null, sources: [] },
                    key => this.translate.instant(key),
                    this.translate.getCurrentLang(),
                ),
            )
            .join(' + '),
        after: formatShoppingListItemMeta(
            { ...group.after, category: null, sources: [] },
            key => this.translate.instant(key),
            this.translate.getCurrentLang(),
        ),
        id: group.after.id,
    }));
    protected close(confirmed = false): void {
        this.ref.close(confirmed);
    }
}
