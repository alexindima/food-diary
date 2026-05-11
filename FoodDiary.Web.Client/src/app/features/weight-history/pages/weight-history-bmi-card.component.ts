import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { BmiStatusInfo } from '../lib/weight-history.facade';
import type { BmiSegmentViewModel } from './weight-history-page.types';

@Component({
    selector: 'fd-weight-history-bmi-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './weight-history-bmi-card.component.html',
    styleUrl: './weight-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryBmiCardComponent {
    public readonly value = input.required<number | null>();
    public readonly status = input.required<BmiStatusInfo | null>();
    public readonly segments = input.required<BmiSegmentViewModel[]>();
    public readonly pointerPosition = input.required<string>();
}
