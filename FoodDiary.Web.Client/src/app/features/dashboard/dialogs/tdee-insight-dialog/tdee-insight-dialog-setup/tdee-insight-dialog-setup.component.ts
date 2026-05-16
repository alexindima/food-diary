import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { TdeeSetupItem } from '../tdee-insight-dialog-lib/tdee-insight-dialog.types';

@Component({
    selector: 'fd-tdee-insight-dialog-setup',
    imports: [FdUiIconComponent, TranslatePipe],
    templateUrl: './tdee-insight-dialog-setup.component.html',
    styleUrl: '../tdee-insight-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogSetupComponent {
    public readonly items = input.required<readonly TdeeSetupItem[]>();
}
