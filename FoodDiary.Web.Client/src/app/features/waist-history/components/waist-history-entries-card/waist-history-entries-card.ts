import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { RECENT_MEASUREMENT_LIMIT } from '../../../../shared/measurements/measurement-history.constants';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { buildWaistEntryViewModels } from '../../lib/waist-history-chart.mapper';
import type { WaistEntry } from '../../models/waist-entry.data';

@Component({
    selector: 'fd-waist-history-entries-card',
    imports: [
        FdUiHintDirective,
        LocalizedNumberPipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        MeasurementUnitPipe,
        MeasurementValuePipe,
        TranslatePipe,
    ],
    templateUrl: './waist-history-entries-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryEntriesCardComponent {
    protected readonly locale = injectCurrentLanguage();
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly translateService = inject(TranslateService);

    public readonly isLoading = input.required<boolean>();
    public readonly entries = input.required<WaistEntry[]>();
    protected readonly items = computed(() => {
        const today = formatDateInputValue(new Date());
        return buildWaistEntryViewModels(this.entries(), resolveTranslateLanguage(this.translateService)).map((item, index, items) => {
            const olderEntry = items.at(index + 1)?.entry;
            return {
                ...item,
                isToday: item.entry.date.startsWith(today),
                change: olderEntry === undefined ? null : item.entry.circumferenceCm - olderEntry.circumferenceCm,
            };
        });
    });
    protected readonly visibleItems = computed(() => this.items().slice(0, RECENT_MEASUREMENT_LIMIT));
    protected readonly canToggleEntries = computed(() => this.items().length > RECENT_MEASUREMENT_LIMIT);

    public readonly editEntry = output<WaistEntry>();
    public readonly removeEntry = output<WaistEntry>();
    public readonly showAllEntries = output();
}
