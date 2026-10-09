import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { CycleFactorId } from '../../../../shared/models/semantics/entity-id';
import { CycleFactorItemComponent } from '../cycle-factor-item/cycle-factor-item';
import type { CycleFactorListItemViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-factor-list',
    imports: [TranslatePipe, CycleFactorItemComponent],
    templateUrl: './cycle-factor-list.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleFactorListComponent {
    public readonly items = input.required<CycleFactorListItemViewModel[]>();
    public readonly isSaving = input(false);
    public readonly editFactor = output<CycleFactorId>();
    public readonly endFactor = output<CycleFactorId>();
}
