import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

@Component({
    selector: 'fd-ai-consent-dialog',
    templateUrl: './ai-consent-dialog.component.html',
    styleUrls: ['./ai-consent-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, TranslatePipe],
})
export class AiConsentDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<AiConsentDialogComponent, boolean>);

    public readonly isAgreed = signal(false);

    public onCheckboxChange(event: Event): void {
        this.isAgreed.set((event.target as HTMLInputElement).checked);
    }

    public onAccept(): void {
        this.dialogRef.close(true);
    }

    public onCancel(): void {
        this.dialogRef.close(false);
    }
}
