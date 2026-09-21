import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementHistoryPager } from '../../../../shared/measurements/measurement-history-pager';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import { buildWaistEntryViewModels } from '../../lib/waist-history-chart.mapper';
import type { WaistEntry } from '../../models/waist-entry.data';

export type WaistHistoryEntriesDialogResult = { action: 'edit' | 'remove'; entry: WaistEntry };

@Component({
    selector: 'fd-waist-history-entries-dialog',
    imports: [
        FdUiHintDirective,
        LocalizedNumberPipe,
        FdUiButtonComponent,
        FdUiDialogShellComponent,
        MeasurementUnitPipe,
        MeasurementValuePipe,
        TranslatePipe,
    ],
    templateUrl: './waist-history-entries-dialog.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryEntriesDialogComponent {
    protected readonly locale = injectCurrentLanguage();
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly dialogRef = inject(FdUiDialogRef<WaistHistoryEntriesDialogComponent, WaistHistoryEntriesDialogResult>);
    private readonly translateService = inject(TranslateService);

    private readonly facade = inject(WaistHistoryFacade);
    protected readonly pager = new MeasurementHistoryPager(dateTo => this.facade.getEntryHistoryPage(dateTo));

    protected readonly items = computed(() => {
        const today = formatDateInputValue(new Date());
        return buildWaistEntryViewModels(this.pager.entries(), resolveTranslateLanguage(this.translateService))
            .map((item, index, items) => {
                const olderEntry = items.at(index + 1)?.entry;
                const change = olderEntry === undefined ? null : item.entry.circumferenceCm - olderEntry.circumferenceCm;
                return { ...item, isToday: item.entry.date.startsWith(today), change };
            })
            .slice(0, this.pager.visibleCount());
    });

    protected select(action: WaistHistoryEntriesDialogResult['action'], entry: WaistEntry): void {
        this.dialogRef.close({ action, entry });
    }
}
