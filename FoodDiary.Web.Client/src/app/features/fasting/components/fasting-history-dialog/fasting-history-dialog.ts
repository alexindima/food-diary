import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, type Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { FastingSession } from '../../../../shared/models/fasting.data';
import type { FastingHistorySessionViewModel } from '../../lib/fasting-page.types';
import { getFastingDurationDisplay } from '../../lib/fasting-session-state';

export type FastingHistoryDialogData = {
    historyItems: Signal<readonly FastingHistorySessionViewModel[]>;
    canLoadMoreHistory: Signal<boolean>;
    isLoadingMoreHistory: Signal<boolean>;
    historyError: Signal<string | null>;
    onSessionOpen: (session: FastingSession) => void;
    onHistoryLoadMore: () => void;
};

@Component({
    selector: 'fd-fasting-history-dialog',
    imports: [
        DatePipe,
        LocalizedNumberPipe,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogFooterDirective,
        FdUiDialogShellComponent,
        FdUiIconComponent,
    ],
    templateUrl: './fasting-history-dialog.html',
    styleUrl: './fasting-history-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingHistoryDialogComponent {
    protected readonly data = inject<FastingHistoryDialogData>(FD_UI_DIALOG_DATA);
    private readonly translateService = inject(TranslateService);
    private readonly dialogRef = inject<FdUiDialogRef<FastingHistoryDialogComponent, void>>(FdUiDialogRef);
    private readonly languageChange = toSignal(this.translateService.onLangChange);

    protected readonly currentLocale = computed(() => {
        this.languageChange();
        return this.translateService.currentLang() ?? 'en';
    });

    protected readonly sessionDurationView = getFastingDurationDisplay;

    protected openSession(session: FastingSession): void {
        this.dialogRef.close();
        this.data.onSessionOpen(session);
    }

    protected close(): void {
        this.dialogRef.close();
    }
}
