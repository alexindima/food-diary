import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { WhtStatusInfo } from '../lib/waist-history.facade';
import type { WhtSegment } from './waist-history-page.types';

@Component({
    selector: 'fd-waist-history-wht-card',
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './waist-history-wht-card.component.html',
    styleUrl: './waist-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryWhtCardComponent {
    public readonly value = input.required<number | null>();
    public readonly status = input.required<WhtStatusInfo | null>();
    public readonly segments = input.required<WhtSegment[]>();
    public readonly pointerPosition = input.required<string>();
}
