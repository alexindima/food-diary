import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import { buildWeightEntryViewModels } from '../../lib/weight-history-chart.mapper';
import type { WeightEntry } from '../../models/weight-entry.data';

const RECENT_ENTRY_LIMIT = 5;

@Component({
    selector: 'fd-weight-history-entries-card',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiCardComponent, TranslatePipe],
    templateUrl: './weight-history-entries-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryEntriesCardComponent {
    private readonly translateService = inject(TranslateService);

    public readonly isLoading = input.required<boolean>();
    public readonly entries = input.required<WeightEntry[]>();
    protected readonly items = computed(() => {
        const today = formatDateInputValue(new Date());
        return buildWeightEntryViewModels(this.entries(), resolveTranslateLanguage(this.translateService)).map((item, index, items) => {
            const olderEntry = items.at(index + 1)?.entry;
            return {
                ...item,
                isToday: item.entry.date.startsWith(today),
                change: olderEntry === undefined ? null : item.entry.weight - olderEntry.weight,
            };
        });
    });
    protected readonly visibleItems = computed(() => this.items().slice(0, RECENT_ENTRY_LIMIT));
    protected readonly canToggleEntries = computed(() => this.items().length > RECENT_ENTRY_LIMIT);

    public readonly editEntry = output<WeightEntry>();
    public readonly removeEntry = output<WeightEntry>();
    public readonly showAllEntries = output<void>();
}
