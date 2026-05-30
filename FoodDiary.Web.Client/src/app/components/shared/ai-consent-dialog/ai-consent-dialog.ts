import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

@Component({
    selector: 'fd-ai-consent-dialog',
    templateUrl: './ai-consent-dialog.html',
    styleUrls: ['./ai-consent-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, TranslatePipe],
})
export class AiConsentDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<AiConsentDialogComponent, boolean>);

    protected readonly isAgreed = signal(false);

    protected onCheckboxChange(event: Event): void {
        if (event.target instanceof HTMLInputElement) {
            this.isAgreed.set(event.target.checked);
        }
    }

    protected onAccept(): void {
        this.dialogRef.close(true);
    }

    protected onCancel(): void {
        this.dialogRef.close(false);
    }
}
