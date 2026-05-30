import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { BmiViewModel } from '../../lib/weight-history.types';

@Component({
    selector: 'fd-weight-history-bmi-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './weight-history-bmi-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryBmiCardComponent {
    public readonly viewModel = input.required<BmiViewModel | null>();
}
