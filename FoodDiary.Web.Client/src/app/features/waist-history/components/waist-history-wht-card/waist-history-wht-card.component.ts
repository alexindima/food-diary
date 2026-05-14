import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { WhtViewModel } from '../../lib/waist-history.types';

@Component({
    selector: 'fd-waist-history-wht-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './waist-history-wht-card.component.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryWhtCardComponent {
    public readonly viewModel = input.required<WhtViewModel | null>();
}
