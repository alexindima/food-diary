import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { buildWeightEntryViewModels } from '../../lib/weight-history-chart.mapper';
import type { WeightEntry } from '../../models/weight-entry.data';

@Component({
    selector: 'fd-weight-history-entries-card',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './weight-history-entries-card.component.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryEntriesCardComponent {
    private readonly translateService = inject(TranslateService);

    public readonly isLoading = input.required<boolean>();
    public readonly entries = input.required<WeightEntry[]>();
    public readonly items = computed(() => buildWeightEntryViewModels(this.entries(), this.translateService.getCurrentLang()));

    public readonly editEntry = output<WeightEntry>();
    public readonly removeEntry = output<WeightEntry>();
}
