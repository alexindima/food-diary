import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementHistoryPager } from '../../../../shared/measurements/measurement-history-pager';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { buildWeightEntryViewModels } from '../../lib/weight-history-chart.mapper';
import { getWeightChangeTone } from '../../lib/weight-history-progress.utils';
import type { WeightEntry } from '../../models/weight-entry.data';

export type WeightHistoryEntriesDialogResult = {
    action: 'edit' | 'remove';
    entry: WeightEntry;
};

export type WeightHistoryEntriesDialogData = {
    currentWeight: number | null;
    desiredWeightKg: number | null;
};

@Component({
    selector: 'fd-weight-history-entries-dialog',
    imports: [
        FdUiHintDirective,
        LocalizedNumberPipe,
        FdUiButtonComponent,
        FdUiDialogShellComponent,
        MeasurementUnitPipe,
        MeasurementValuePipe,
        TranslatePipe,
    ],
    templateUrl: './weight-history-entries-dialog.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryEntriesDialogComponent {
    protected readonly locale = injectCurrentLanguage();
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly data = inject<WeightHistoryEntriesDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject(FdUiDialogRef<WeightHistoryEntriesDialogComponent, WeightHistoryEntriesDialogResult>);
    private readonly translateService = inject(TranslateService);

    private readonly facade = inject(WeightHistoryFacade);
    protected readonly pager = new MeasurementHistoryPager(dateTo => this.facade.getEntryHistoryPage(dateTo));

    protected readonly items = computed(() => {
        const today = formatDateInputValue(new Date());
        return buildWeightEntryViewModels(this.pager.entries(), resolveTranslateLanguage(this.translateService))
            .map((item, index, items) => {
                const olderEntry = items.at(index + 1)?.entry;
                const change = olderEntry === undefined ? null : item.entry.weightKg - olderEntry.weightKg;
                return {
                    ...item,
                    isToday: item.entry.date.startsWith(today),
                    change,
                    tone: getWeightChangeTone(change, this.data.currentWeight, this.data.desiredWeightKg),
                };
            })
            .slice(0, this.pager.visibleCount());
    });

    protected select(action: WeightHistoryEntriesDialogResult['action'], entry: WeightEntry): void {
        this.dialogRef.close({ action, entry });
    }
}
