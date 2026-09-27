import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

import { AppearanceFacade } from './appearance.facade';

export type { AppearanceDialogData } from './appearance.facade';

@Component({
    selector: 'fd-appearance-dialog',
    templateUrl: './appearance-dialog.html',
    styleUrl: './appearance-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogComponent, FdUiSegmentedToggleComponent],
    providers: [AppearanceFacade],
})
export class AppearanceDialogComponent {
    protected readonly appearance = inject(AppearanceFacade);
}
