import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { buildWaistEntryViewModels } from '../../lib/waist-history-chart.mapper';
import type { WaistEntry } from '../../models/waist-entry.data';

@Component({
    selector: 'fd-waist-history-entries-card',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './waist-history-entries-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryEntriesCardComponent {
    private readonly translateService = inject(TranslateService);

    public readonly isLoading = input.required<boolean>();
    public readonly entries = input.required<WaistEntry[]>();
    protected readonly items = computed(() => buildWaistEntryViewModels(this.entries(), this.translateService.getCurrentLang()));

    public readonly editEntry = output<WaistEntry>();
    public readonly removeEntry = output<WaistEntry>();
}
