import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { WaistEntry } from '../models/waist-entry.data';
import type { WaistEntryViewModel } from './waist-history-page.types';

@Component({
    selector: 'fd-waist-history-entries-card',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './waist-history-entries-card.component.html',
    styleUrl: './waist-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryEntriesCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<WaistEntryViewModel[]>();

    public readonly editEntry = output<WaistEntry>();
    public readonly removeEntry = output<WaistEntry>();
}
