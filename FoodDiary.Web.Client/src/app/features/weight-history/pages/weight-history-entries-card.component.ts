import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { WeightEntry } from '../models/weight-entry.data';
import type { WeightEntryViewModel } from './weight-history-page.types';

@Component({
    selector: 'fd-weight-history-entries-card',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './weight-history-entries-card.component.html',
    styleUrl: './weight-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryEntriesCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<WeightEntryViewModel[]>();

    public readonly editEntry = output<WeightEntry>();
    public readonly removeEntry = output<WeightEntry>();
}
