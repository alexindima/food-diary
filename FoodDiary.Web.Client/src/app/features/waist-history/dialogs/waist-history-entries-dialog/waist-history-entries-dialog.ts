import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import { buildWaistEntryViewModels } from '../../lib/waist-history-chart.mapper';
import type { WaistEntry } from '../../models/waist-entry.data';

export type WaistHistoryEntriesDialogResult = { action: 'edit' | 'remove'; entry: WaistEntry };
export type WaistHistoryEntriesDialogData = { entries: WaistEntry[]; desiredWaist: number | null };

@Component({
    selector: 'fd-waist-history-entries-dialog',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiDialogShellComponent, TranslatePipe],
    templateUrl: './waist-history-entries-dialog.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryEntriesDialogComponent {
    private readonly data = inject<WaistHistoryEntriesDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject(FdUiDialogRef<WaistHistoryEntriesDialogComponent, WaistHistoryEntriesDialogResult>);
    private readonly translateService = inject(TranslateService);

    protected readonly items = computed(() => {
        const today = formatDateInputValue(new Date());
        return buildWaistEntryViewModels(this.data.entries, resolveTranslateLanguage(this.translateService)).map((item, index, items) => {
            const olderEntry = items.at(index + 1)?.entry;
            const change = olderEntry === undefined ? null : item.entry.circumference - olderEntry.circumference;
            return { ...item, isToday: item.entry.date.startsWith(today), change };
        });
    });

    protected select(action: WaistHistoryEntriesDialogResult['action'], entry: WaistEntry): void {
        this.dialogRef.close({ action, entry });
    }
}
