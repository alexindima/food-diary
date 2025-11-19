import { CommonModule } from '@angular/common';
import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  inject,
  input,
  contentChild
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FdUiDialogFooterDirective } from './fd-ui-dialog-footer.directive';

export type FdUiDialogSize = 'sm' | 'md' | 'lg';

export interface FdUiDialogData {
    title?: string;
    subtitle?: string;
    size?: FdUiDialogSize;
    dismissible?: boolean;
}

@Component({
    selector: 'fd-ui-dialog',
    standalone: true,
    imports: [CommonModule, MatIconModule],
    templateUrl: './fd-ui-dialog.component.html',
    styleUrls: ['./fd-ui-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
})
export class FdUiDialogComponent {
    private readonly dialogRef = inject(MatDialogRef<FdUiDialogComponent>, { optional: true });
    private readonly injectedData = inject(MAT_DIALOG_DATA, { optional: true }) as FdUiDialogData | null;

    private readonly footerSlot = contentChild(FdUiDialogFooterDirective);

    public readonly title = input<string | undefined>(this.injectedData?.title);
    public readonly subtitle = input<string | undefined>(this.injectedData?.subtitle);
    public readonly size = input<FdUiDialogSize>(this.injectedData?.size ?? 'md');
    public readonly dismissible = input(this.injectedData?.dismissible ?? true, {
        transform: booleanAttribute,
    });

    public readonly showHeader = computed(() => Boolean(this.title() || this.subtitle() || this.dismissible()));

    public get hasFooter(): boolean {
        return Boolean(this.footerSlot());
    }

    public close(result?: unknown): void {
        this.dialogRef?.close(result);
    }
}
