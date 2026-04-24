import { CommonModule } from '@angular/common';
import {
    booleanAttribute,
    ChangeDetectionStrategy,
    Component,
    ViewEncapsulation,
    computed,
    inject,
    input,
    contentChild,
} from '@angular/core';
import { FD_UI_DIALOG_DATA } from './fd-ui-dialog-data';
import { FdUiDialogRef } from './fd-ui-dialog-ref';
import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { FdUiDialogFooterDirective } from './fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from './fd-ui-dialog-header.directive';

let nextDialogId = 0;

export type FdUiDialogSize = 'sm' | 'md' | 'lg' | 'xl';

export interface FdUiDialogData {
    title?: string;
    subtitle?: string;
    size?: FdUiDialogSize;
    dismissible?: boolean;
}

@Component({
    selector: 'fd-ui-dialog',
    standalone: true,
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-dialog.component.html',
    styleUrls: ['./fd-ui-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
})
export class FdUiDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<FdUiDialogComponent>, { optional: true });
    private readonly injectedData = inject(FD_UI_DIALOG_DATA, { optional: true }) as FdUiDialogData | null;

    public readonly dialogTitleId = `fd-dialog-title-${nextDialogId++}`;

    private readonly footerSlot = contentChild(FdUiDialogFooterDirective, { descendants: true });
    private readonly headerSlot = contentChild(FdUiDialogHeaderDirective, { descendants: true });

    public readonly title = input<string | undefined>(this.injectedData?.title);
    public readonly subtitle = input<string | undefined>(this.injectedData?.subtitle);
    public readonly size = input<FdUiDialogSize>(this.injectedData?.size ?? 'md');
    public readonly dismissible = input(this.injectedData?.dismissible ?? true, {
        transform: booleanAttribute,
    });

    public readonly showHeader = computed(() => Boolean(this.title() || this.subtitle() || this.dismissible()));
    public readonly hasCustomHeader = computed(() => Boolean(this.headerSlot()));
    public readonly showBuiltInHeader = computed(() => this.showHeader() && !this.hasCustomHeader());

    public get hasFooter(): boolean {
        return Boolean(this.footerSlot());
    }

    public close(result?: unknown): void {
        this.dialogRef?.close(result);
    }
}
