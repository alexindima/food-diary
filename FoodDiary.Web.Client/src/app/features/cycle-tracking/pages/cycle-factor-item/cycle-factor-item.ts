import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { CycleFactorListItemViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-factor-item',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './cycle-factor-item.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleFactorItemComponent {
    public readonly factor = input.required<CycleFactorListItemViewModel>();
    public readonly isSaving = input(false);
    public readonly editFactor = output<string>();
    public readonly endFactor = output<string>();
}
