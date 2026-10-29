import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { map } from 'rxjs';

import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import type { DashboardHydrationHistory } from '../../../../shared/models/dashboard.data';

@Component({
    selector: 'fd-hydration-details-dialog',
    imports: [TranslatePipe, LocalizedDatePipe, LocalizedNumberPipe, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
    templateUrl: './hydration-details-dialog.html',
    styleUrl: './hydration-details-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HydrationDetailsDialogComponent {
    private readonly translate = inject(TranslateService);
    protected readonly dialogRef = inject<FdUiDialogRef<HydrationDetailsDialogComponent, boolean>>(FdUiDialogRef);
    protected readonly data = inject<{ total: number; history?: DashboardHydrationHistory | null }>(FD_UI_DIALOG_DATA);
    protected readonly language = toSignal(this.translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translate),
    });
}
