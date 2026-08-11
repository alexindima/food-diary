import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, type Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { MS_PER_HOUR } from '../../../../shared/lib/time.constants';
import type { FastingSession } from '../../models/fasting.data';
import type { FastingHistorySessionViewModel } from '../../pages/fasting-page-lib/fasting-page.types';

export type FastingHistoryDialogData = {
    historyItems: Signal<readonly FastingHistorySessionViewModel[]>;
    canLoadMoreHistory: Signal<boolean>;
    isLoadingMoreHistory: Signal<boolean>;
    onSessionOpen: (session: FastingSession) => void;
    onHistoryLoadMore: () => void;
};

@Component({
    selector: 'fd-fasting-history-dialog',
    imports: [
        DatePipe,
        DecimalPipe,
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
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly dialogRef = inject<FdUiDialogRef<FastingHistoryDialogComponent, void>>(FdUiDialogRef);
    private readonly languageVersion = signal(0);

    protected readonly currentLocale = computed(() => {
        this.languageVersion();
        return this.translateService.currentLang() ?? 'en';
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected sessionDurationHours(session: FastingSession): number {
        const end = session.endedAtUtc === null ? new Date() : new Date(session.endedAtUtc);
        return Math.max(0, (end.getTime() - new Date(session.startedAtUtc).getTime()) / MS_PER_HOUR);
    }

    protected openSession(session: FastingSession): void {
        this.dialogRef.close();
        this.data.onSessionOpen(session);
    }

    protected close(): void {
        this.dialogRef.close();
    }
}
