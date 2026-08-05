import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import { buildWeightEntryViewModels } from '../../lib/weight-history-chart.mapper';
import type { WeightEntry } from '../../models/weight-entry.data';

export type WeightHistoryEntriesDialogResult = {
    action: 'edit' | 'remove';
    entry: WeightEntry;
};

@Component({
    selector: 'fd-weight-history-entries-dialog',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiDialogShellComponent, TranslatePipe],
    templateUrl: './weight-history-entries-dialog.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryEntriesDialogComponent {
    private readonly entries = inject<WeightEntry[]>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject(FdUiDialogRef<WeightHistoryEntriesDialogComponent, WeightHistoryEntriesDialogResult>);
    private readonly translateService = inject(TranslateService);

    protected readonly items = computed(() => {
        const today = formatDateInputValue(new Date());
        return buildWeightEntryViewModels(this.entries, resolveTranslateLanguage(this.translateService)).map((item, index, items) => {
            const olderEntry = items.at(index + 1)?.entry;
            return {
                ...item,
                isToday: item.entry.date.startsWith(today),
                change: olderEntry === undefined ? null : item.entry.weight - olderEntry.weight,
            };
        });
    });

    protected select(action: WeightHistoryEntriesDialogResult['action'], entry: WeightEntry): void {
        this.dialogRef.close({ action, entry });
    }
}
